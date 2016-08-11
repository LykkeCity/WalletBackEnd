using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.Policy;
using NBitcoin.RPC;
using Newtonsoft.Json;
using ServiceLykkeWallet.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;
using static LykkeWalletServices.OpenAssetsHelper;

namespace ServiceLykkeWallet.Controllers
{
    public class TransactionsController : ApiController
    {
        public class UnsignedTransaction
        {
            public long Id
            {
                get;
                set;
            }

            public string TransactionHex
            {
                get;
                set;
            }
        }

        public class TransferRequest
        {
            public string SourceAddress
            {
                get;
                set;
            }

            public string DestinationAddress
            {
                get;
                set;
            }

            public double Amount
            {
                get;
                set;
            }

            public string Asset
            {
                get;
                set;
            }
        }

        public class TranctionSignAndBroadcastRequest
        {
            public long Id
            {
                get;
                set;
            }

            public string ClientSignedTransaction
            {
                get;
                set;
            }
        }

        public class TransactionSignRequest
        {
            public string TransactionToSign
            {
                get;
                set;
            }

            public string PrivateKey
            {
                get;
                set;
            }
        }

        public class TransactionSignResponse
        {
            public string SignedTransaction
            {
                get;
                set;
            }
        }

        // This should respond to curl -H "Content-Type: application/json" -X POST -d "{\"SourceAddress\":\"xyz\",\"DestinationAddress\":\"xyz\", \"Amount\":10.25, \"Asset\":\"TestExchangeUSD\"}" http://localhost:8989/Transactions/CreateUnsignedTransfer
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> CreateUnsignedTransfer(TransferRequest transferRequest)
        {
            IHttpActionResult result = null;

            try
            {
                var ret = await CreateUnsignedTransferWorker(transferRequest);
                if (ret.Item2 == null)
                {
                    result = Json(ret.Item1);
                }
                else
                {
                    result = InternalServerError(new Exception(ret.Item2.ToString()));
                }
            }
            catch (Exception e)
            {
                result = InternalServerError(e);
            }

            await OpenAssetsHelper.SendPendingEmailsAndLogInputOutput
                (WebSettings.ConnectionString, "Transfer:" + JsonConvert.SerializeObject(transferRequest), ConvertResultToString(result));
            return result;
        }

        public async Task<string> SignTransactionWorker(TransactionSignRequest signRequest)
        {
            Transaction tx = new Transaction(signRequest.TransactionToSign);
            Transaction outputTx = new Transaction(signRequest.TransactionToSign);
            var secret = new BitcoinSecret(signRequest.PrivateKey);

            TransactionBuilder builder = new TransactionBuilder();
            tx = builder.ContinueToBuild(tx).AddKeys(new BitcoinSecret[] { secret }).SignTransaction(tx);

            for (int i = 0; i < tx.Inputs.Count; i++)
            {
                var input = tx.Inputs[i];
                var txResponse = await OpenAssetsHelper.GetTransactionHex(input.PrevOut.Hash.ToString(), WebSettings.ConnectionParams);
                if (txResponse.Item1)
                {
                    throw new Exception(string.Format("Error while retrieving transaction {0}, error is: {1}",
                        input.PrevOut.Hash.ToString(), txResponse.Item2));
                }

                ///var builder = new TransactionBuilder();

                var prevTransaction = new Transaction(txResponse.Item3);
                var output = prevTransaction.Outputs[input.PrevOut.N];
                if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                {
                    var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig).RedeemScript;
                    if (PayToMultiSigTemplate.Instance.CheckScriptPubKey(redeemScript))
                    {
                        var pubkeys = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript).PubKeys;
                        for (int j = 0; j < pubkeys.Length; j++)
                        {
                            if (secret.PubKey.ToHex() == pubkeys[j].ToHex())
                            {
                                var scriptParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig);
                                var hash = Script.SignatureHash(scriptParams.RedeemScript, tx, i, SigHash.All);
                                var signature = secret.PrivateKey.Sign(hash, SigHash.All);
                                scriptParams.Pushes[j + 1] = signature.Signature.ToDER().Concat(new byte[] { 0x01 }).ToArray();
                                outputTx.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(scriptParams);
                            }
                        }
                    }
                    continue;
                }

                if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                {
                    var address = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(output.ScriptPubKey).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToWif();
                    if (address == secret.GetAddress().ToWif())
                    {
                        var hash = Script.SignatureHash(output.ScriptPubKey, tx, i, SigHash.All);
                        var signature = secret.PrivateKey.Sign(hash, SigHash.All);

                        outputTx.Inputs[i].ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(signature, secret.PubKey);
                    }

                    continue;
                }
            }

            /*
            for (int i = 0; i < outputTx.Inputs.Count; i++)
            {
                var input = outputTx.Inputs[i];
                var txResponse = await OpenAssetsHelper.GetTransactionHex(input.PrevOut.Hash.ToString(),
                    WebSettings.ConnectionParams);
                if (txResponse.Item1)
                {
                    throw new Exception(string.Format("Error while retrieving transaction {0}, error is: {1}",
                        input.PrevOut.Hash.ToString(), txResponse.Item2));
                }

                builder.AddCoins(new Transaction(txResponse.Item3));
            }
            var verify = builder.Verify(outputTx);
            */

            return outputTx.ToHex();
        }

        // This should respond to curl -H "Content-Type: application/json" -X POST -d "{\"Id\":4371,\"ClientSignedTransaction\":\"xxx\"}" http://localhost:8989/Transactions/SignTransactionIfRequiredAndBroadcast
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> SignTransactionIfRequiredAndBroadcast(TranctionSignAndBroadcastRequest signBroadcastRequest)
        {
            IHttpActionResult result = null;

            Transaction finalTransaction = null;

            if (signBroadcastRequest.Id < 0)
            {
                result = BadRequest("Id of the transaction should not be negative.");
            }
            else
            {
                try
                {
                    using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                    {
                        using (var dbTransaction = entities.Database.BeginTransaction())
                        {
                            /*
                            var txRecord = (from transaction in entities.SentTransactions
                                            where transaction.id == (int)signBroadcastRequest.Id
                                            select transaction).FirstOrDefault();
                            */
                            var txRecord = (from transaction in entities.UnsignedTransactions
                                            where transaction.id == (int)signBroadcastRequest.Id
                                            select transaction).FirstOrDefault();

                            if (txRecord == null)
                            {
                                result = BadRequest(string.Format("The request transaction with id:{0} was not found.", signBroadcastRequest.Id));
                            }
                            else
                            {
                                if (txRecord.HasTimedout == true)
                                {
                                    result = BadRequest(string.Format("The request transaction with id:{0} has been timeout, please request a new transaction.", signBroadcastRequest.Id));
                                }
                                else
                                {
                                    if (txRecord.TransactionIdWhichMadeThisTransactionInvalid != null)
                                    {
                                        result = BadRequest(string.Format("The request transaction with id:{0} has been become invalid by another transaction.", signBroadcastRequest.Id));
                                    }
                                    else
                                    {
                                        if (txRecord.TransactionSendingSuccessful ?? false)
                                        {
                                            string txId = null;
                                            if (!string.IsNullOrEmpty(txRecord.ExchangeSignedTransactionAfterClient))
                                            {
                                                txId = (new Transaction(txRecord.ExchangeSignedTransactionAfterClient)).GetHash().ToString();
                                            }
                                            else
                                            {
                                                txId = (new Transaction(txRecord.ClientSignedTransaction)).GetHash().ToString();
                                            }
                                            result = BadRequest(string.Format("This transaction has been successfully sent before with Bitcoin transaction id: {0} .", txId));
                                        }
                                        else
                                        {

                                            if (!(txRecord.IsClientSignatureRequired ?? false))
                                            {
                                                result = BadRequest(string.Format("The requested transaction with id:{0} does not require client side signature.", signBroadcastRequest.Id));
                                            }
                                            else
                                            {
                                                txRecord.ClientSignedTransaction = signBroadcastRequest.ClientSignedTransaction;
                                                finalTransaction = new Transaction(signBroadcastRequest.ClientSignedTransaction);
                                                await entities.SaveChangesAsync();

                                                if (txRecord.IsExchangeSignatureRequired ?? false)
                                                {
                                                    TransactionSignRequest request = new TransactionSignRequest
                                                    {
                                                        TransactionToSign = txRecord.TransactionHex,
                                                        PrivateKey = WebSettings.ExchangePrivateKey
                                                    };

                                                    var exchangeSignResult = await SignTransactionWorker(request);

                                                    var unsignedTx = new Transaction(txRecord.TransactionHex);
                                                    var clientSignedTx = new Transaction(signBroadcastRequest.ClientSignedTransaction);
                                                    var exchangeSignedTx = new Transaction(exchangeSignResult);
                                                    /*
                                                    TransactionBuilder builder = new TransactionBuilder();
                                                    finalTransaction = builder.ContinueToBuild(new Transaction(txRecord.TransactionHex)).CombineSignatures(new Transaction[] { new Transaction(txRecord.TransactionHex),
                                                        new Transaction(signBroadcastRequest.ClientSignedTransaction),
                                                        new Transaction(exchangeSignResult) });
                                                        */

                                                    for (int i = 0; i < unsignedTx.Inputs.Count; i++)
                                                    {
                                                        var input = unsignedTx.Inputs[i];
                                                        var txResponse = await OpenAssetsHelper.GetTransactionHex(input.PrevOut.Hash.ToString(), WebSettings.ConnectionParams);
                                                        if (txResponse.Item1)
                                                        {
                                                            throw new Exception(string.Format("Error while retrieving transaction {0}, error is: {1}",
                                                                input.PrevOut.Hash.ToString(), txResponse.Item2));
                                                        }

                                                        var prevTransaction = new Transaction(txResponse.Item3);
                                                        var output = prevTransaction.Outputs[input.PrevOut.N];
                                                        if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                                                        {
                                                            var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig).RedeemScript;
                                                            if (PayToMultiSigTemplate.Instance.CheckScriptPubKey(redeemScript))
                                                            {
                                                                var scriptParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig);

                                                                var clientPushes = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(clientSignedTx.Inputs[i].ScriptSig).Pushes;
                                                                var exchangePushes = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(exchangeSignedTx.Inputs[i].ScriptSig).Pushes;

                                                                //scriptParams.Pushes = new byte[][] { clientPushes[1] , exchangePushes[2] };


                                                                scriptParams.Pushes[1] = clientPushes[1];
                                                                scriptParams.Pushes[2] = exchangePushes[2];


                                                                finalTransaction.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(scriptParams);
                                                                /*
                                                                var pubkeys = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript).PubKeys;
                                                                for (int j = 0; j < pubkeys.Length; j++)
                                                                {
                                                                    if (secret.PubKey.ToHex() == pubkeys[j].ToHex())
                                                                    {
                                                                        var scriptParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig);
                                                                        scriptParams.Pushes[j + 1] = tx.SignInput(secret, new Coin(prevTransaction, output)).Signature.ToDER();
                                                                        outputTx.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(scriptParams);
                                                                    }
                                                                }
                                                                */
                                                            }
                                                        }
                                                    }

                                                    TransactionBuilder builder = new TransactionBuilder();
                                                    for (int i = 0; i < finalTransaction.Inputs.Count; i++)
                                                    {
                                                        var input = finalTransaction.Inputs[i];
                                                        var txResponse = await OpenAssetsHelper.GetTransactionHex(input.PrevOut.Hash.ToString(), WebSettings.ConnectionParams);
                                                        if (txResponse.Item1)
                                                        {
                                                            throw new Exception(string.Format("Error while retrieving transaction {0}, error is: {1}",
                                                                input.PrevOut.Hash.ToString(), txResponse.Item2));
                                                        }

                                                        builder.AddCoins(new Transaction(txResponse.Item3));
                                                    }

                                                    //MinerTransactionPolicy.Instance.
                                                    var verified = builder.Verify(finalTransaction);

                                                    txRecord.ExchangeSignedTransactionAfterClient = finalTransaction.ToHex();
                                                    await entities.SaveChangesAsync();
                                                }

                                                try
                                                {
                                                    /*
                                                    RPCClient client = new RPCClient(new System.Net.NetworkCredential(WebSettings.ConnectionParams.Username, WebSettings.ConnectionParams.Password),
                                                        WebSettings.ConnectionParams.IpAddress, WebSettings.ConnectionParams.BitcoinNetwork);

                                                    await client.SendRawTransactionAsync(finalTransaction);
                                                    */
                                                    await PerformProperOperationForCollidingTransactions(entities, txRecord);
                                                    await entities.SaveChangesAsync();

                                                    var sendingRetValue = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt(finalTransaction, WebSettings.ConnectionParams,
                                                        entities, WebSettings.ConnectionString, null, null);

                                                    await MakeReservedFeesAsSpent(entities, finalTransaction, txRecord.OwnerAddress);

                                                    txRecord.TransactionId = finalTransaction.GetHash().ToString();
                                                    txRecord.TransactionSendingSuccessful = true;
                                                    await entities.SaveChangesAsync();

                                                    result = Ok(finalTransaction.GetHash().ToString());

                                                    dbTransaction.Commit();
                                                }
                                                catch (Exception e)
                                                {
                                                    txRecord.TransactionSendingSuccessful = false;
                                                    txRecord.TransactionSendingError = e.ToString();
                                                    result = InternalServerError(e);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    result = InternalServerError(e);
                }
            }

            await OpenAssetsHelper.SendPendingEmailsAndLogInputOutput
                (WebSettings.ConnectionString, "Transfer:" + JsonConvert.SerializeObject(signBroadcastRequest), ConvertResultToString(result));

            return result;
        }

        private async Task PerformProperOperationForCollidingTransactions(SqlexpressLykkeEntities entities,
            LykkeWalletServices.UnsignedTransaction txRecord)
        {
            var sameOwnerTransactions = (from tx in entities.UnsignedTransactions
                                         where
                                         tx.OwnerAddress == txRecord.OwnerAddress &&
                                         (tx.HasTimedout ?? false) == false &&
                                         tx.TransactionIdWhichMadeThisTransactionInvalid == null &&
                                         (tx.TransactionSendingSuccessful ?? false ) == false
                                         select tx).ToList();

            IList<LykkeWalletServices.UnsignedTransaction> invalidTransactionList = new List<LykkeWalletServices.UnsignedTransaction>();

            foreach(var transaction in sameOwnerTransactions)
            {
                if(transaction.id == txRecord.id)
                {
                    continue;
                }

                if(MakesInvalidTransaction(new Transaction(txRecord.TransactionHex), new Transaction(transaction.TransactionHex)))
                {
                    invalidTransactionList.Add(transaction);
                    transaction.TransactionIdWhichMadeThisTransactionInvalid = txRecord.id;
                }
            }
            await entities.SaveChangesAsync();

            var spentOutptuts = txRecord.UnsignedTransactionSpentOutputs;

            for (int i = 0; i < invalidTransactionList.Count(); i++)
            {
                var record = invalidTransactionList[i];

                var freedOutput = (from output in entities.UnsignedTransactionSpentOutputs
                                   where output.UnsignedTransactionId == record.id
                                   select output).ToList();

                foreach (var item in freedOutput)
                {
                    if(spentOutptuts.Where(o => o.TransactionId == item.TransactionId && o.OutputNumber == item.OutputNumber).Any())
                    {
                        // The item has been spent by current transaction
                        continue;
                    }

                    var transactionsWithFreedOutput = (from output in entities.UnsignedTransactionSpentOutputs
                                                       join tr in entities.UnsignedTransactions on output.UnsignedTransactionId equals tr.id
                                                       where output.TransactionId == item.TransactionId &&
                                                       output.OutputNumber == item.OutputNumber &&
                                                       (tr.HasTimedout ?? false) == false &&
                                                       tr.TransactionIdWhichMadeThisTransactionInvalid == null
                                                       select output.UnsignedTransactionId).Count();

                    if (transactionsWithFreedOutput > 0)
                    {
                        continue;
                    }
                    else
                    {
                        await CheckForFeeOutputAndFree(entities, item);
                        await entities.SaveChangesAsync();
                    }
                }
            }


        }

        private static bool MakesInvalidTransaction(Transaction spendingTransaction, Transaction beingSpentTransaction)
        {
            var spendingInputs = spendingTransaction.Inputs;
            var beingSpentInputs = beingSpentTransaction.Inputs;
            foreach(var spendingInput in spendingInputs)
            {
                foreach(var beingSpentInput in beingSpentInputs)
                {
                    if(spendingInput.PrevOut.Hash == beingSpentInput.PrevOut.Hash && spendingInput.PrevOut.N == spendingInput.PrevOut.N)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ToDo: Possible performance increase with join operation
        private async Task MakeReservedFeesAsSpent(SqlexpressLykkeEntities entities, Transaction finalTransaction, string ownerAddress)
        {
            var feesForAddress = (from fee in entities.PreGeneratedOutputs
                                  where fee.Consumed == 0 && fee.ReservedForAddress == ownerAddress
                                  select fee).ToList();

            var txInputs = finalTransaction.Inputs.Select(i => new { Hash = i.PrevOut.Hash, N = i.PrevOut.N });
            for (int i = 0; i < feesForAddress.Count; i++)
            {
                var fee = feesForAddress[i];
                if (txInputs.Where(input => (input.Hash.ToString() == fee.TransactionId && input.N == fee.OutputNumber)).Any())
                {
                    fee.Consumed = 1;
                }
            }

            await entities.SaveChangesAsync();
        }

        // This should respond to curl -H "Content-Type: application/json" -X POST -d "{\"TransactionToSign\":\"xyz\",\"PrivateKey\":\"xyz\"}" http://localhost:8989/Transactions/SignTransaction
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> SignTransaction(TransactionSignRequest signRequest)
        {
            IHttpActionResult result = null;
            try
            {
                TransactionSignResponse response = new TransactionSignResponse
                { SignedTransaction = await SignTransactionWorker(signRequest) };
                result = Json(response);
            }
            catch (Exception e)
            {
                result = InternalServerError(e);
            }

            return result;
        }

        public string ConvertResultToString(IHttpActionResult result)
        {
            if (result is ExceptionResult)
            {
                return (result as ExceptionResult).Exception.ToString();
            }

            if (result is JsonResult<UnsignedTransaction>)
            {
                return JsonConvert.SerializeObject((result as JsonResult<UnsignedTransaction>).Content);
            }

            if (result is BadRequestErrorMessageResult)
            {
                return ((BadRequestErrorMessageResult)result).Message;
            }

            if (result is OkNegotiatedContentResult<string>)
            {
                return ((OkNegotiatedContentResult<string>)result).Content;
            }

            return result.ToString();
        }

        public async Task<Tuple<UnsignedTransaction, Error>> CreateUnsignedTransferWorker(TransferRequest data)
        {
            UnsignedTransaction result = null;
            Error error = null;
            bool isClientSignatureRequired = false;
            bool isExchangeSignatureRequired = false;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.SourceAddress);
                    if (sourceAddress == null)
                    {
                        error = new Error();
                        error.Code = ErrorCode.InvalidAddress;
                        error.Message = "Invalid source address provided";
                    }
                    else
                    {
                        var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.DestinationAddress);
                        if (destAddress == null)
                        {
                            error = new Error();
                            error.Code = ErrorCode.InvalidAddress;
                            error.Message = "Invalid destination address provided";
                        }
                        else
                        {
                            KeyStorage SourceMultisigAddress = null;
                            if (sourceAddress is BitcoinScriptAddress)
                            {
                                SourceMultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.SourceAddress, entities);
                            }

                            OpenAssetsHelper.GetCoinsForWalletReturnType walletCoins = null;
                            if (sourceAddress is BitcoinPubKeyAddress)
                            {
                                walletCoins = (OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, data.Asset.GetAssetBTCAmount(data.Amount), data.Amount, data.Asset,
                                    WebSettings.Assets, WebSettings.ConnectionParams, WebSettings.ConnectionString, entities, true, false);
                            }
                            else
                            {
                                walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, data.Asset.GetAssetBTCAmount(data.Amount), data.Amount, data.Asset,
                                    WebSettings.Assets, WebSettings.ConnectionParams, WebSettings.ConnectionString, entities, false);
                            }
                            if (walletCoins.Error != null)
                            {
                                error = walletCoins.Error;
                            }
                            else
                            {
                                using (var transaction = entities.Database.BeginTransaction())
                                {
                                    Coin[] uncoloredCoins = null;
                                    TransactionBuilder builder = new TransactionBuilder();
                                    builder
                                        .SetChange(sourceAddress, ChangeType.Colored);
                                    if (sourceAddress is BitcoinPubKeyAddress)
                                    {
                                        isClientSignatureRequired = true;
                                        isExchangeSignatureRequired = false;

                                        if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                        {
                                            builder.AddCoins(((OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)walletCoins).AssetCoins);
                                        }
                                        else
                                        {
                                            uncoloredCoins = ((OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)walletCoins).Coins;
                                            builder.AddCoins(uncoloredCoins);
                                        }
                                    }
                                    else
                                    {
                                        isClientSignatureRequired = true;
                                        isExchangeSignatureRequired = true;

                                        if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                        {
                                            builder.AddCoins(((OpenAssetsHelper.GetScriptCoinsForWalletReturnType)walletCoins).AssetScriptCoins);
                                        }
                                        else
                                        {
                                            uncoloredCoins = ((OpenAssetsHelper.GetScriptCoinsForWalletReturnType)walletCoins).ScriptCoins;
                                            builder.AddCoins(uncoloredCoins);
                                        }
                                    }

                                    var reservedId = Guid.NewGuid().ToString();

                                    if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                    {
                                        builder = (await builder.SendAsset(destAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, WebSettings.ConnectionParams.BitcoinNetwork)),
                                            Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                                        .AddEnoughPaymentFee(entities, WebSettings.ConnectionParams,
                                        WebSettings.FeeAddress, 2, -1, data.SourceAddress, reservedId));
                                    }
                                    else
                                    {
                                        builder.SendWithChange(destAddress,
                                            Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor),
                                            uncoloredCoins,
                                            sourceAddress);
                                        builder = (await builder.AddEnoughPaymentFee(entities, WebSettings.ConnectionParams,
                                            WebSettings.FeeAddress, 0, -1, data.SourceAddress, reservedId));
                                    }

                                    var tx = builder.BuildTransaction(true);

                                    var txHash = tx.GetHash().ToString();

                                    OpenAssetsHelper.SentTransactionReturnValue transactionResult = null;

                                    var unsignedTransaction = entities.UnsignedTransactions.Add(
                                        new LykkeWalletServices.UnsignedTransaction
                                        {
                                            IsExchangeSignatureRequired = isExchangeSignatureRequired,
                                            IsClientSignatureRequired = true,
                                            TransactionHex = tx.ToHex(),
                                            OwnerAddress = sourceAddress.ToWif(),
                                            CreationTime = DateTime.UtcNow
                                        });
                                    await entities.SaveChangesAsync();

                                    foreach (var unspentOutput in tx.Inputs)
                                    {
                                        entities.UnsignedTransactionSpentOutputs.Add
                                            (new UnsignedTransactionSpentOutput { TransactionId = unspentOutput.PrevOut.Hash.ToString(), OutputNumber = (int)unspentOutput.PrevOut.N ,
                                                UnsignedTransactionId = unsignedTransaction.id });
                                    }
                                    await entities.SaveChangesAsync();

                                    result = new UnsignedTransaction
                                    {
                                        TransactionHex = tx.ToHex(),
                                        Id = unsignedTransaction.id
                                    };

                                    /*
                                    if (!isExchangeSignatureRequired)
                                    {
                                        transactionResult = await OpenAssetsHelper.CheckTransactionForDoubleSpentClientSignatureRequired
                                        (tx, WebSettings.ConnectionParams, entities, WebSettings.ConnectionString, null, null);
                                    }
                                    else
                                    {
                                        transactionResult = await OpenAssetsHelper.CheckTransactionForDoubleSpentBothSignaturesRequired
                                            (tx, WebSettings.ConnectionParams, entities, WebSettings.ConnectionString, null, null);
                                    }


                                    if (transactionResult.Error == null)
                                    {
                                        result = new UnsignedTransaction
                                        {
                                            TransactionHex = tx.ToHex(),
                                            Id = transactionResult.SentTransactionId
                                        };
                                    }
                                    else
                                    {
                                        error = transactionResult.Error;
                                    }
                                    */
                                    if (error == null)
                                    {
                                        transaction.Commit();
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                    }
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<UnsignedTransaction, Error>(result, error);
        }
    }
}
