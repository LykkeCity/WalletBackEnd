using LykkeWalletServices;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web.Http;
using static LykkeWalletServices.OpenAssetsHelper;

namespace ServiceLykkeWallet.Controllers
{
    public class OffchainController : ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> AddClientProvidedChannelInput(string multiSig, string transactionId)
        {
            return Ok();
        }

        [HttpGet]
        public async Task<IHttpActionResult> GenerateUnsignedChannelSetupTransaction(string clientPubkey, double clientContributedAmount,
            string hubPubkey, double hubContributedAmount, string channelAssetName, int channelTimeoutInMinutes)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                var multisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);
                var asset = OpenAssetsHelper.GetAssetFromName(WebSettings.Assets, channelAssetName,
                    WebSettings.ConnectionParams.BitcoinNetwork);
                if (asset == null && channelAssetName.ToLower() != "btc")
                {
                    return InternalServerError(new Exception(string.Format("The specified asset is not supported {0}.",
                        channelAssetName)));
                }

                var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(multisig.MultiSigAddress, WebSettings.ConnectionParams.BitcoinNetwork,
                    entities);
                if (walletOutputs.Item2 && !(walletOutputs?.Item3 ?? string.Empty).ToLower().StartsWith("no coins "))
                {
                    return InternalServerError(new Exception(string.Format
                        ("Error in getting outputs for wallet: {0}, the error is {1}", multisig.MultiSigAddress, walletOutputs.Item3)));
                }
                else
                {
                    if (walletOutputs.Item4)
                    {
                        // We should not reach probably by using LykkkeBlockchainManager
                        // ToDo: Clarification required
                        return InternalServerError(new Exception(string.Format("Some inputs are in race with a refund for address: {0}",
                            multisig.MultiSigAddress)));
                    }
                    else
                    {
                        var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, asset.AssetId);
                        var coins = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, asset.AssetId);
                        long totalAmount = 0;
                        if (asset == null)
                        {
                            totalAmount = coins.Item2.Sum(c => c.Amount);
                        }
                        else
                        {
                            totalAmount = coins.Item1.Sum(c => c.Amount.Quantity);
                        }

                        return await GenerateUnsignedChannelSetupTransactionCore(clientPubkey, clientContributedAmount, hubPubkey,
                            hubContributedAmount, totalAmount / asset.AssetMultiplicationFactor, channelAssetName, channelTimeoutInMinutes);
                    }
                }
            }
        }
        
        [HttpGet]
        public async Task<IHttpActionResult> GenerateUnsignedChannelSetupTransactionCore(string clientPubkey,
            double clientContributedAmount, string hubPubkey, double hubContributedAmount, double multisigNewlyAddedAmount,
            string channelAssetName, int channelTimeoutInMinutes)
        {
            try
            {
                string txHex = null;
                var btcAsset = (channelAssetName.ToLower() == "btc");
                var clientAddress = (new PubKey(clientPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                var hubAddress = (new PubKey(hubPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                var multisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);

                var asset = WebSettings.Assets.Where(a => a.Name == channelAssetName).FirstOrDefault();
                OpenAssetsHelper.GetAssetFromName(WebSettings.Assets, channelAssetName, WebSettings.ConnectionParams.BitcoinNetwork);
                if (asset == null && channelAssetName.ToLower() != "btc")
                {
                    return InternalServerError(new Exception(string.Format("The specified asset is not supported {0}.", channelAssetName)));
                }
                var assetId = asset?.AssetId;
                var assetMultiplyFactor = asset?.MultiplyFactor ?? (long)OpenAssetsHelper.BTCToSathoshiMultiplicationFactor;

                long[] contributedAmount = new long[3];
                long[] requiredAssetAmount = new long[3];
                requiredAssetAmount[0] = (long)(clientContributedAmount * assetMultiplyFactor);
                requiredAssetAmount[1] = (long)(multisigNewlyAddedAmount * asset.MultiplyFactor);
                requiredAssetAmount[2] = (long)(hubContributedAmount * assetMultiplyFactor);

                IList<ICoin>[] coinToBeUsed = new IList<ICoin>[3];
                BitcoinAddress[] inputAddress = new BitcoinAddress[3];
                double[] inputAmount = new double[3];

                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    /*
                    if (string.IsNullOrEmpty(ClientMultisigAddress))
                    {
                        var multisig = await OpenAssetsHelper
                            .GetMatchingMultisigAddress(ClientMultisigAddress, entities);

                        var parameters = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters
                            (new Script(multisig.MultiSigScript));
                        if (!string.IsNullOrEmpty(ClientAddress) && !DoesPubKeyMatchesAddress(parameters.PubKeys[0], ClientAddress))
                        {
                            return InternalServerError(new Exception
                                (string.Format(multsigMismatchAddressError, "client")));
                        }
                        if (!string.IsNullOrEmpty(HubAddress) && !DoesPubKeyMatchesAddress(parameters.PubKeys[1], ClientAddress))
                        {
                            return InternalServerError(new Exception
                                (string.Format(multsigMismatchAddressError, "Hub")));
                        }
                    }
                    */

                    for (int i = 0; i < 3; i++)
                    {
                        coinToBeUsed[i] = new List<ICoin>();
                        switch (i)
                        {
                            case 0:
                                inputAddress[i] = clientAddress;
                                inputAmount[i] = clientContributedAmount;
                                break;
                            case 1:
                                inputAddress[i] = Base58Data.GetFromBase58Data(multisig.MultiSigAddress) as BitcoinScriptAddress;
                                inputAmount[i] = multisigNewlyAddedAmount;
                                break;
                            case 2:
                                inputAddress[i] = hubAddress;
                                inputAmount[i] = hubContributedAmount;
                                break;
                        }

                        var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(inputAddress[i].ToString(), WebSettings.ConnectionParams.BitcoinNetwork,
                            entities);
                        if (walletOutputs.Item2 && !(walletOutputs?.Item3 ?? string.Empty).ToLower().StartsWith("no coins "))
                        {
                            return InternalServerError(new Exception(string.Format("Error in getting outputs for wallet: {0}, the error is {1}",
                                inputAddress, walletOutputs.Item3)));
                        }
                        else
                        {
                            if (walletOutputs.Item4)
                            {
                                // We should not reach probably by using LykkkeBlockchainManager
                                // ToDo: Clarification required
                                return InternalServerError(new Exception(string.Format("Some inputs are in race with a refund for address: {0}",
                                    inputAddress)));
                            }

                            var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, assetId);
                            ICoin[] coinToSelectFrom;
                            if (btcAsset)
                            {
                                coinToSelectFrom = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, assetId).Item2;
                            }
                            else
                            {
                                coinToSelectFrom = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, assetId).Item1;
                            }

                            contributedAmount[i] = 0;

                            if (requiredAssetAmount[i] > 0)
                            {
                                foreach (var item in coinToSelectFrom)
                                {
                                    if (btcAsset)
                                    {
                                        contributedAmount[i] += ((Coin)item).Amount;
                                    }
                                    else
                                    {
                                        contributedAmount[i] += ((ColoredCoin)item).Amount.Quantity;
                                    }

                                    if (i == 1)
                                    {
                                        if (btcAsset)
                                        {
                                            var scriptItem =
                                                new ScriptCoin((Coin)item, new Script(multisig.MultiSigScript));
                                            coinToBeUsed[i].Add(item);
                                        }
                                        else
                                        {
                                            var bearer = ((ColoredCoin)item).Bearer;
                                            var scriptBearer =
                                                new ScriptCoin(bearer, new Script(multisig.MultiSigScript));
                                            var coloredScriptCoin = new ColoredCoin(((ColoredCoin)item).Amount, scriptBearer);
                                            coinToBeUsed[i].Add(coloredScriptCoin);
                                        }
                                    }
                                    else
                                    {
                                        coinToBeUsed[i].Add(item);
                                    }

                                    if (contributedAmount[i] >= requiredAssetAmount[i])
                                    {
                                        break;
                                    }
                                }
                            }

                            if (contributedAmount[i] < requiredAssetAmount[i])
                            {
                                return InternalServerError(new Exception(string.Format("Address {0} has not {1} of {2}.",
                                    inputAddress[i], inputAmount[i], channelAssetName)));
                            }
                        }
                    } // end of for

                    TransactionBuilder builder = new TransactionBuilder();
                    for (int i = 0; i < 3; i++)
                    {
                        builder.AddCoins(coinToBeUsed[i]);
                    }

                    var directSendValue = 0L;
                    var returnValue = 0L;

                    var numberOfColoredCoinOutputs = 0;
                    var multisigAddress = Base58Data.GetFromBase58Data(multisig.MultiSigAddress) as BitcoinAddress;

                    for (int i = 0; i < 3; i++)
                    {
                        directSendValue = requiredAssetAmount[i];
                        returnValue = contributedAmount[i] - requiredAssetAmount[i];

                        if (directSendValue <= 0)
                        {
                            continue;
                        }

                        if (btcAsset)
                        {
                            if (returnValue > 0)
                            {
                                builder.Send(inputAddress[i], new Money(returnValue));
                            }
                        }
                        else
                        {
                            if (returnValue > 0)
                            {
                                builder.SendAsset(inputAddress[i], new AssetMoney(new AssetId(new BitcoinAssetId(assetId)),
                                    returnValue));
                                numberOfColoredCoinOutputs++;
                            }
                        }
                    }

                    var directSendSum = requiredAssetAmount.Sum();
                    if (directSendSum > 0)
                    {
                        if (btcAsset)
                        {
                            builder.Send(multisigAddress, new Money(directSendSum));
                        }
                        else
                        {
                            builder.SendAsset(multisigAddress,
                                new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), directSendSum));
                            numberOfColoredCoinOutputs++;
                        }
                    }

                    using (var transaction = entities.Database.BeginTransaction())
                    {
                        var now = DateTime.UtcNow;
                        var reservationEndDate =
                            (channelTimeoutInMinutes == 0 ? now.AddYears(1000) : now.AddMinutes(channelTimeoutInMinutes));

                        await builder.AddEnoughPaymentFee(entities, WebSettings.ConnectionParams, WebSettings.FeeAddress,
                            numberOfColoredCoinOutputs, -1, "Offchain" + multisig.MultiSigAddress, Guid.NewGuid().ToString(),
                            reservationEndDate);
                        txHex = builder.BuildTransaction(true).ToHex();
                        var txHash = Convert.ToString(SHA256Managed.Create().ComputeHash(StringToByteArray(txHex)));
                        var channel = entities.OffchainChannels.Add(new OffchainChannel { unsignedTransactionHash = txHash });
                        await entities.SaveChangesAsync();

                        for (int i = 0; i < 3; i++)
                        {
                            string toBeStoredTxId = null;
                            int toBeStoredTxOutputNumber = 0;

                            foreach (var item in coinToBeUsed[i])
                            {
                                if (btcAsset)
                                {
                                    toBeStoredTxId = ((Coin)item).Outpoint.Hash.ToString();
                                    toBeStoredTxOutputNumber = (int)((Coin)item).Outpoint.N;
                                }
                                else
                                {
                                    toBeStoredTxId = ((ColoredCoin)item).Bearer.Outpoint.Hash.ToString();
                                    toBeStoredTxOutputNumber = (int)((ColoredCoin)item).Bearer.Outpoint.N;
                                }


                                var coin = new ChannelCoin
                                {
                                    OffchainChannel = channel,
                                    TransactionId = toBeStoredTxId,
                                    OutputNumber = toBeStoredTxOutputNumber,
                                    ReservationCreationDate = now,
                                    ReservedForChannel = channel.ChannelId,
                                    ReservedForMultisig = multisig.MultiSigAddress,
                                    ReservationEndDate = reservationEndDate
                                };
                                entities.ChannelCoins.Add(coin);
                            }
                        }

                        await entities.SaveChangesAsync();
                        transaction.Commit();
                    }
                }
                return Json(new UnsignedChannelSetupTransaction { UnsigndTransaction = txHex });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        private static bool DoesPubKeyMatchesAddress(PubKey pubKey, string address)
        {
            var oneSideAddress = pubKey.GetAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();
            if (oneSideAddress != address)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> CreateUnsignedClientCommitmentTransaction(string UnsignedChannelSetupTransaction,
            string ClientSignedChannelSetup, double clientCommitedAmount, double hubCommitedAmount, string clientPubkey,
            string hubPrivatekey, string assetName, string counterPartyRevokePubkey, int activationIn10Minutes)
        {
            try
            {
                Transaction unsignedTx = new Transaction(UnsignedChannelSetupTransaction);
                Transaction clientSignedTx = new Transaction(ClientSignedChannelSetup);

                var hubSecret = new BitcoinSecret(hubPrivatekey);
                var hubPubkey = hubSecret.PubKey.ToString();

                var clientSignedVersionOK = await CheckIfClientSignedVersionIsOK(unsignedTx, clientSignedTx,
                    new PubKey(clientPubkey), new PubKey(hubPubkey));
                if (!clientSignedVersionOK.Success)
                {
                    return InternalServerError(new Exception(clientSignedVersionOK.ErrorMessage));
                }

                var fullySignedSetup = await SignTransactionWorker(new TransactionSignRequest
                {
                    PrivateKey = hubPrivatekey,
                    TransactionToSign = clientSignedTx.ToHex()
                });

                string errorMessage = null;
                var unsignedCommitmentTx = CreateUnsignnedCommitmentTransaction(fullySignedSetup, clientCommitedAmount,
                    hubCommitedAmount, clientPubkey, hubPubkey, assetName, true, counterPartyRevokePubkey, activationIn10Minutes,
                    out errorMessage);

                return Json(new UnsignedClientCommitmentTransactionResponse
                {
                    FullySignedSetupTransaction = fullySignedSetup,
                    UnsignedClientCommitment0 = unsignedCommitmentTx
                });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        private static Script CreateSpecialCommitmentScript(string counterPartyPubkey, string selfPubkey,
            string counterPartyRevokePubkey, int activationIn10Minutes)
        {
            var multisigScriptOps = PayToMultiSigTemplate.Instance.GenerateScriptPubKey
                (2, new PubKey[] { new PubKey(selfPubkey), new PubKey(counterPartyRevokePubkey) }).ToOps();
            List<Op> ops = new List<Op>();
            ops.Add(OpcodeType.OP_IF);
            ops.AddRange(multisigScriptOps);
            ops.Add(OpcodeType.OP_ELSE);
            ops.Add(Op.GetPushOp(serialize(activationIn10Minutes)));
            ops.Add(OpcodeType.OP_CHECKSEQUENCEVERIFY);
            ops.Add(OpcodeType.OP_DROP);
            ops.Add(Op.GetPushOp(OpenAssetsHelper.StringToByteArray(counterPartyPubkey)));
            ops.Add(OpcodeType.OP_CHECKSIG);
            ops.Add(OpcodeType.OP_ENDIF);

            return new Script(ops.ToArray());
        }

        // Copied from NBitcoin source code
        // If not used probably the error: "non-minimally encoded script number" will be arised while verifing the transaction
        public static byte[] serialize(long value)
        {
            if (value == 0)
                return new byte[0];

            var result = new List<byte>();
            bool neg = value < 0;
            long absvalue = neg ? -value : value;

            while (absvalue != 0)
            {
                result.Add((byte)(absvalue & 0xff));
                absvalue >>= 8;
            }

            //    - If the most significant byte is >= 0x80 and the value is positive, push a
            //    new zero-byte to make the significant byte < 0x80 again.

            //    - If the most significant byte is >= 0x80 and the value is negative, push a
            //    new 0x80 byte that will be popped off when converting to an integral.

            //    - If the most significant byte is < 0x80 and the value is negative, add
            //    0x80 to it, since it will be subtracted and interpreted as a negative when
            //    converting to an integral.

            if ((result[result.Count - 1] & 0x80) != 0)
                result.Add((byte)(neg ? 0x80 : 0));
            else if (neg)
                result[result.Count - 1] |= 0x80;

            return result.ToArray();
        }

        public string CreateUnsignnedCommitmentTransaction(string fullySignedSetup, double clientContributedAmount,
            double hubContributedAmount, string clientPubkey, string hubPubkey, string assetName, bool isClientToHub,
            string counterPartyRevokePubKey, int activationIn10Minutes, out string errorMessage)
        {
            var multisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);
            var asset = GetAssetFromName(WebSettings.Assets, assetName, WebSettings.ConnectionParams.BitcoinNetwork);
            var btcAsset = (assetName.ToLower() == "btc");

            TransactionBuilder builder = new TransactionBuilder();

            long totalInputSatoshi = 0;
            Transaction fullySignedTx = new Transaction(fullySignedSetup);
            for (uint i = 0; i < fullySignedTx.Outputs.Count; i++)
            {
                if (fullySignedTx.Outputs[i].ScriptPubKey
                    .GetDestinationAddress(WebSettings.ConnectionParams.BitcoinNetwork)?.ToString() == multisig.MultiSigAddress)
                {
                    totalInputSatoshi += fullySignedTx.Outputs[i].Value.Satoshi;
                    if (btcAsset)
                    {
                        if (fullySignedTx.Outputs[i].Value
                            != (long)((clientContributedAmount + hubContributedAmount) * BTCToSathoshiMultiplicationFactor))
                        {
                            errorMessage =
                                string.Format("The btc values in multisig output does not much sum of the input parameters {0} and {1}."
                                , clientContributedAmount, hubContributedAmount);
                            return null;
                        }

                        builder.AddCoins(new ScriptCoin(new Coin(fullySignedTx, i), new Script(multisig.MultiSigScript)));
                    }
                    else
                    {
                        // ToDo: Curretnly we trust that clientCommitedAmount + hubCommitedAmount to be equal to colored output
                        // In future it is better to drop this trust and like LykkeBitcoinBlockchainManager in CSPK
                        var bearer = new ScriptCoin(new Coin(fullySignedTx, i), new Script(multisig.MultiSigScript));
                        builder.AddCoins(new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(asset.AssetId)), (long)((clientContributedAmount + hubContributedAmount) * asset.AssetMultiplicationFactor)), bearer));
                    }
                    break;
                }
            }

            var dummyCoinToBeRemoved = new Coin(new uint256(0), 0,
                new Money(1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor),
                (new PubKey(hubPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork).ScriptPubKey);
            builder.AddCoins(dummyCoinToBeRemoved);
            totalInputSatoshi += (long)(1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);

            var clientAddress = (new PubKey(clientPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
            var hubAddress = (new PubKey(hubPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);

            long totalOutputSatoshi = 0;
            long outputSatoshi = 0;

            IDestination clientDestination = null;
            IDestination hubDestination = null;
            if (!isClientToHub)
            {
                // If it is the transaction hub is sending to the client, in case of broadcast hub should get the funds immediatly
                clientDestination = CreateSpecialCommitmentScript(clientPubkey, hubPubkey, counterPartyRevokePubKey,
                    activationIn10Minutes).GetScriptAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                hubDestination = hubAddress;
            }
            else
            {
                clientDestination = clientAddress;
                hubDestination = CreateSpecialCommitmentScript(hubPubkey, clientPubkey, counterPartyRevokePubKey,
                    activationIn10Minutes).GetScriptAddress(WebSettings.ConnectionParams.BitcoinNetwork);
            }

            if (btcAsset)
            {
                if (clientContributedAmount > 0)
                {
                    outputSatoshi = (long)(clientContributedAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);
                    builder.Send(clientDestination,
                        new Money((long)(outputSatoshi)));
                    totalOutputSatoshi += outputSatoshi;
                }

                if (hubContributedAmount > 0)
                {
                    outputSatoshi = (long)(hubContributedAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);
                    builder.Send(hubDestination,
                        new Money(outputSatoshi));
                    totalOutputSatoshi += outputSatoshi;
                }
            }
            else
            {
                if (clientContributedAmount > 0)
                {
                    builder.SendAsset(clientDestination, new AssetMoney(new AssetId(new BitcoinAssetId(asset.AssetId)),
                        (long)(clientContributedAmount * asset.AssetMultiplicationFactor)));
                    totalOutputSatoshi += (new TxOut(Money.Zero, clientAddress.ScriptPubKey).GetDustThreshold
                        (new FeeRate(Money.Satoshis(5000)))).Satoshi;
                }

                if (hubContributedAmount > 0)
                {
                    builder.SendAsset(hubDestination, new AssetMoney(new AssetId(new BitcoinAssetId(asset.AssetId)),
                        (long)(hubContributedAmount * asset.AssetMultiplicationFactor)));
                    totalOutputSatoshi += (new TxOut(Money.Zero, hubAddress.ScriptPubKey).GetDustThreshold
                        (new FeeRate(Money.Satoshis(5000)))).Satoshi;
                }
            }

            builder.SendFees(new Money(totalInputSatoshi - totalOutputSatoshi));
            errorMessage = null;
            var tx = builder.BuildTransaction(true, SigHash.All | SigHash.AnyoneCanPay);

            TxIn toBeRemovedInput = null;
            foreach (var item in tx.Inputs)
            {
                if (item.PrevOut.Hash == new uint256(0))
                {
                    toBeRemovedInput = item;
                    break;
                }
            }
            if (toBeRemovedInput != null)
            {
                tx.Inputs.Remove(toBeRemovedInput);
            }

            return tx.ToHex();
        }

        public class GeneralCallResult
        {
            public bool Success
            {
                get;
                set;
            }

            public string ErrorMessage
            {
                get;
                set;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="usignedTx"></param>
        /// <param name="clientSignedTx"></param>
        /// <returns>Null return value means the client signature is as expected</returns>
        private static async Task<GeneralCallResult> CheckIfClientSignedVersionIsOK(Transaction unsignedTx,
            Transaction clientSignedTx, PubKey clientPubkey, PubKey hubPubkey, SigHash sigHash = SigHash.All)
        {
            string errorMessage = null;

            if (unsignedTx.Inputs.Count != clientSignedTx.Inputs.Count)
            {
                errorMessage = "The input size for the client signed transaction does not match the expected value.";
                return new GeneralCallResult { Success = false, ErrorMessage = errorMessage };
            }

            if (unsignedTx.Outputs.Count != clientSignedTx.Outputs.Count)
            {
                errorMessage = "The output size for the client signed transaction does not matched the expected value.";
                return new GeneralCallResult { Success = false, ErrorMessage = errorMessage };
            }

            for (int i = 0; i < unsignedTx.Inputs.Count; i++)
            {
                if (unsignedTx.Inputs[i].PrevOut.Hash != clientSignedTx.Inputs[i].PrevOut.Hash)
                {
                    errorMessage = string.Format("For the input {0} the previous transaction hashes do not match.", i);
                    return new GeneralCallResult { Success = false, ErrorMessage = errorMessage };
                }

                if (unsignedTx.Inputs[i].PrevOut.N != clientSignedTx.Inputs[i].PrevOut.N)
                {
                    errorMessage = string.Format("For the input {0} the previous transaction output numbers do not match.", i);
                    return new GeneralCallResult { Success = false, ErrorMessage = errorMessage };
                }
            }

            var hasPubkeySignedCorrectly = await HasPubkeySignedTransactionCorrectly(unsignedTx, clientSignedTx,
                clientPubkey, hubPubkey, sigHash);

            return hasPubkeySignedCorrectly;
        }

        private static async Task<GeneralCallResult> HasPubkeySignedTransactionCorrectly(Transaction unsignedTransaction,
            Transaction signedTransaction, PubKey clientPubkey, PubKey hubPubkey, SigHash sigHash = SigHash.All)
        {
            var multiSig = GetMultiSigFromTwoPubKeys(clientPubkey.ToString(), hubPubkey.ToString());

            for (int i = 0; i < signedTransaction.Inputs.Count; i++)
            {
                var input = signedTransaction.Inputs[i];
                var txResponse = await OpenAssetsHelper.GetTransactionHex(input.PrevOut.Hash.ToString(),
                    WebSettings.ConnectionParams);
                if (txResponse.Item1)
                {
                    return new GeneralCallResult
                    {
                        Success = false,
                        ErrorMessage = string.Format("Error while retrieving transaction {0}, error is: {1}",
                        input.PrevOut.Hash.ToString(), txResponse.Item2)
                    };
                }

                var prevTransaction = new Transaction(txResponse.Item3);
                var output = prevTransaction.Outputs[input.PrevOut.N];

                if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                {
                    if (clientPubkey.GetAddress(WebSettings.ConnectionParams.BitcoinNetwork) ==
                        output.ScriptPubKey.GetDestinationAddress(WebSettings.ConnectionParams.BitcoinNetwork))
                    {
                        var clientSignedSignature = PayToPubkeyHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig).
                            TransactionSignature.Signature;

                        var sig = Script.SignatureHash(output.ScriptPubKey, unsignedTransaction, i, sigHash);
                        var verified = clientPubkey.Verify(sig, clientSignedSignature);
                        if (!verified)
                        {
                            return new GeneralCallResult
                            {
                                Success = false,
                                ErrorMessage =
                                string.Format("Expected signature was not present for input {0}.", i)
                            };
                        }
                    }
                }
                else
                {
                    if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(output.ScriptPubKey))
                    {
                        var redeemScript = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig).RedeemScript;
                        if (PayToMultiSigTemplate.Instance.CheckScriptPubKey(redeemScript))
                        {
                            var pubkeys = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(redeemScript).PubKeys;
                            for (int j = 0; j < pubkeys.Length; j++)
                            {
                                if (clientPubkey.ToString() == pubkeys[j].ToHex())
                                {
                                    var scriptParams = PayToScriptHashTemplate.Instance.ExtractScriptSigParameters(input.ScriptSig);
                                    var hash = Script.SignatureHash(scriptParams.RedeemScript, unsignedTransaction, i, sigHash);

                                    var verified = clientPubkey.Verify(hash, scriptParams.Pushes[j + 1]);
                                    if (!verified)
                                    {
                                        return new GeneralCallResult
                                        {
                                            Success = false,
                                            ErrorMessage =
                                            string.Format("Expected signature was not present for input {0}.", i)
                                        };
                                    }

                                    /*
                                    var signature = secret.PrivateKey.Sign(hash, sigHash);
                                    scriptParams.Pushes[j + 1] = signature.Signature.ToDER().Concat(new byte[] { (byte)sigHash }).ToArray();
                                    outputTx.Inputs[i].ScriptSig = PayToScriptHashTemplate.Instance.GenerateScriptSig(scriptParams);
                                    */
                                }
                            }
                        }
                    }
                    else
                    {
                        // We should normall not reach here
                        return new GeneralCallResult
                        {
                            Success = false,
                            ErrorMessage = string.Format("Unsupported scriptpubkey for input {0}.", i)
                        };
                    }
                }
            }

            return new GeneralCallResult { Success = true, ErrorMessage = "Script verified successfully." };
        }
                
        [HttpGet]
        public async Task<IHttpActionResult> FinalizeChannelSetup(string FullySignedSetupTransaction, string SignedClientCommitment0,
            double clientCommitedAmount, double hubCommitedAmount, string clientPubkey, string hubPrivatekey, string assetName,
            string clientSelfRevokePubkey, string hubSelfRevokePubkey, int activationIn10Minutes)
        {
            try
            {
                string errorMessage = null;
                var hubPubkey = (new BitcoinSecret(hubPrivatekey)).PubKey;
                var unsignedClientCommitment = CreateUnsignnedCommitmentTransaction(FullySignedSetupTransaction, clientCommitedAmount, hubCommitedAmount,
                clientPubkey, hubPubkey.ToString(), assetName, true, hubSelfRevokePubkey, activationIn10Minutes, out errorMessage);

                if (errorMessage != null)
                {
                    return InternalServerError(new Exception(errorMessage));
                }

                var checkResult = await CheckIfClientSignedVersionIsOK(new Transaction(unsignedClientCommitment), new Transaction(SignedClientCommitment0), new PubKey(clientPubkey), hubPubkey,
                    SigHash.All | SigHash.AnyoneCanPay);
                if (!checkResult.Success)
                {
                    return InternalServerError(new Exception(checkResult.ErrorMessage));
                }

                errorMessage = null;
                var unsignedHubCommitment = CreateUnsignnedCommitmentTransaction(FullySignedSetupTransaction, clientCommitedAmount,
                    hubCommitedAmount, clientPubkey, hubPubkey.ToString(), assetName, false, clientSelfRevokePubkey,
                    activationIn10Minutes, out errorMessage);
                if (errorMessage != null)
                {
                    return InternalServerError(new Exception(errorMessage));
                }

                var signedHubCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    PrivateKey = hubPrivatekey,
                    TransactionToSign = unsignedHubCommitment
                }, SigHash.All | SigHash.AnyoneCanPay);

                return Json(new FinalizeChannelSetupResponse { SignedHubCommitment0 = signedHubCommitment });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> SignCommitment(string unsignedCommitment, string privateKey)
        {
            var signedCommitment = await OpenAssetsHelper.SignTransactionWorker(new OpenAssetsHelper.TransactionSignRequest
            {
                PrivateKey = privateKey,
                TransactionToSign = unsignedCommitment
            }, SigHash.All | SigHash.AnyoneCanPay);

            return Json(new SignCommitmentResponse { SignedCommitment = signedCommitment });
        }

        [HttpGet]
        public async Task<IHttpActionResult> CreateUnsignedCommitmentTransactions(string signedSetupTransaction,
            string clientPubkey, string hubPubkey, double clientAmount, double hubAmount, string assetName,
            string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub)
        {
            try
            {
                string errorMessage = null;
                var unsignedCommitment = CreateUnsignnedCommitmentTransaction(signedSetupTransaction, clientAmount, hubAmount,
                clientPubkey, hubPubkey.ToString(), assetName, clientSendsCommitmentToHub, lockingPubkey, activationIn10Minutes,
                out errorMessage);

                if (errorMessage != null)
                {
                    return InternalServerError(new Exception(errorMessage));
                }
                else
                {
                    return Json(new CreateUnsignedCommitmentTransactionsResponse { UnsignedCommitment = unsignedCommitment });
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> CheckHalfSignedCommitmentTransactionToBeCorrect(string halfSignedCommitment,
            string signedSetupTransaction, string clientPubkey, string hubPubkey, double clientAmount, double hubAmount,
            string assetName, string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub)
        {
            try
            {
                string errorMessage = null;
                var unsignedClientCommitment = CreateUnsignnedCommitmentTransaction(signedSetupTransaction, clientAmount,
                    hubAmount, clientPubkey, hubPubkey.ToString(), assetName, clientSendsCommitmentToHub,
                    lockingPubkey, activationIn10Minutes, out errorMessage);

                if (errorMessage != null)
                {
                    return InternalServerError(new Exception(errorMessage));
                }

                var checkResult = await CheckIfClientSignedVersionIsOK(new Transaction(unsignedClientCommitment),
                    new Transaction(halfSignedCommitment), new PubKey(clientPubkey), new PubKey(hubPubkey),
                    SigHash.All | SigHash.AnyoneCanPay);
                if (!checkResult.Success)
                {
                    return InternalServerError(new Exception(checkResult.ErrorMessage));
                }
                else
                {
                    return Ok();
                }
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> CreateCommitmentSpendingTransactionForMultisigPart(string commitmentTransactionHex, string clientPubkey,
            string hubPubkey, string assetName, string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub,
            string selfPrivateKey, string counterPartyRevokePrivateKey)
        {
            return await CreateCommitmentSpendingTransactionCore(commitmentTransactionHex, null, clientPubkey, hubPubkey, assetName,
                lockingPubkey, activationIn10Minutes, clientSendsCommitmentToHub,
                GenerateCustomeScriptMultisigScriptOutputSpender, selfPrivateKey, counterPartyRevokePrivateKey);
        }

        public TxIn GenerateCustomScriptTimeActivateOutputSpender(TxIn input, Transaction tx, int activationIn10Minutes, Coin bearer,
            Script redeemScript, SigHash sigHash, string spendingPrivateKey, string selfPrivateKey, string counterPartyRevokePrivateKey)
        {
            input.Sequence = new Sequence(activationIn10Minutes);

            var secret = new BitcoinSecret(spendingPrivateKey);
            var signature = tx.SignInput(secret, new Coin(input.PrevOut.Hash, input.PrevOut.N, new Money(bearer.Amount), redeemScript), sigHash);
            var p2shScript = PayToScriptHashTemplate.Instance.GenerateScriptSig(new PayToScriptHashSigParameters
            {
                RedeemScript = redeemScript,
                Pushes = new byte[][] { signature.ToBytes(), new byte[] { ((byte)0) } }
            });
            input.ScriptSig = p2shScript;

            return input;
        }

        public TxIn GenerateCustomeScriptMultisigScriptOutputSpender(TxIn input, Transaction tx, int activationIn10Minutes, Coin bearer,
            Script redeemScript, SigHash sigHash, string spendingPrivateKey, string selfPrivateKey, string counterPartyRevokePrivateKey)
        {
            var selfSecret = new BitcoinSecret(selfPrivateKey);
            var counterPartyRevokeSecret = new BitcoinSecret(counterPartyRevokePrivateKey);

            var selfSignature = tx.SignInput(selfSecret, new Coin(input.PrevOut.Hash, input.PrevOut.N, new Money(bearer.Amount), redeemScript), sigHash);
            var counterPartyRevokeSignature = tx.SignInput(counterPartyRevokeSecret, new Coin(input.PrevOut.Hash, input.PrevOut.N, new Money(bearer.Amount), redeemScript), sigHash);

            var p2shScript = PayToScriptHashTemplate.Instance.GenerateScriptSig(new PayToScriptHashSigParameters
            {
                RedeemScript = redeemScript,
                Pushes = new byte[][] {
                    new byte[] { },
                    selfSignature.ToBytes(),
                    counterPartyRevokeSignature.ToBytes(),
                    new byte[] { ((byte)1) } }
            });
            input.ScriptSig = p2shScript;

            return input;
        }


        public async Task<IHttpActionResult> CreateCommitmentSpendingTransactionCore(string commitmentTransactionHex,
            string spendingPrivateKey, string clientPubkey, string hubPubkey, string assetName,
            string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub,
            Func<TxIn, Transaction, int, Coin, Script, SigHash, string, string, string, TxIn> generateProperOutputSpender,
            string selfPrivateKey, string counterPartyRevokePrivateKey)
        {
            try
            {
                var commtimentTransaction = new Transaction(commitmentTransactionHex);

                string counterPartyPubkey = null;
                string selfPubkey = null;
                if (clientSendsCommitmentToHub)
                {
                    counterPartyPubkey = hubPubkey;
                    selfPubkey = clientPubkey;
                }
                else
                {
                    counterPartyPubkey = clientPubkey;
                    selfPubkey = hubPubkey;
                }

                var scriptToSearch = CreateSpecialCommitmentScript(counterPartyPubkey, selfPubkey,
                    lockingPubkey, activationIn10Minutes);

                TxOut outputToUse = null;
                int outputNumber = 0;
                string multisigAddress = null;
                for (int i = 0; i < commtimentTransaction.Outputs.Count; i++)
                {
                    var output = commtimentTransaction.Outputs[i];
                    if (output.ScriptPubKey.ToString() == scriptToSearch.PaymentScript.ToString())
                    {
                        outputToUse = output;
                        outputNumber = i;
                        multisigAddress = output.ScriptPubKey.GetDestinationAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();
                        break;
                    }
                }

                if (outputToUse == null)
                {
                    return InternalServerError(new Exception("Proper output to spend was not found."));
                }

                var dummyMultisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    using (var transaction = entities.Database.BeginTransaction())
                    {
                        var walletOutputs = await GetWalletOutputs(multisigAddress,
                        WebSettings.ConnectionParams.BitcoinNetwork, entities);
                        if (walletOutputs.Item2)
                        {
                            return InternalServerError(new Exception(walletOutputs.Item3));
                        }
                        else
                        {
                            var commitmentHash = commtimentTransaction.GetHash().ToString();
                            ColoredCoin inputColoredCoin = null;
                            Coin inputCoin = null;

                            var redeemScript = CreateSpecialCommitmentScript(counterPartyPubkey, selfPubkey, lockingPubkey, activationIn10Minutes);
                            Coin bearer = null;
                            foreach (var item in walletOutputs.Item1)
                            {
                                if (item.GetTransactionHash() == commitmentHash
                                    && item.GetOutputIndex() == outputNumber)
                                {
                                    bearer = new Coin(commtimentTransaction, (uint)outputNumber);
                                    ScriptCoin scriptCoin = new ScriptCoin(bearer, redeemScript);

                                    if (IsRealAsset(assetName))
                                    {
                                        inputColoredCoin = new ColoredCoin(
                                            new AssetMoney(new AssetId(new BitcoinAssetId(item.GetAssetId())), item.GetAssetAmount()),
                                            scriptCoin);
                                    }
                                    else
                                    {
                                        inputCoin = scriptCoin;
                                    }
                                }
                            }

                            if (inputColoredCoin == null && inputCoin == null)
                            {
                                return InternalServerError(new Exception("Some errors occured while creating input coin to be consumed"));
                            }
                            else
                            {
                                BitcoinPubKeyAddress destAddress = null;
                                if (spendingPrivateKey != null)
                                {
                                    destAddress = (new PubKey(counterPartyPubkey)).
                                      GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                                }
                                else
                                {
                                    if (clientSendsCommitmentToHub)
                                    {
                                        destAddress = (new PubKey(selfPubkey)).
                                          GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                                    }
                                    else
                                    {
                                        destAddress = (new PubKey(hubPubkey)).
                                            GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                                    }
                                }

                                TransactionBuilder builder = new TransactionBuilder();
                                if (IsRealAsset(assetName))
                                {
                                    var coloredCoinToBeAdded = inputColoredCoin;
                                    builder.AddCoins(coloredCoinToBeAdded);
                                    builder.SendAsset(destAddress, coloredCoinToBeAdded.Amount);
                                }
                                else
                                {
                                    var coinToBeAdded = inputCoin;
                                    builder.AddCoins(coinToBeAdded);
                                    builder.Send(destAddress, coinToBeAdded.Amount);
                                }

                                await builder.AddEnoughPaymentFee
                                    (entities, WebSettings.ConnectionParams, WebSettings.FeeAddress);
                                var tx = builder.BuildTransaction(false);
                                tx.Version = 2;

                                var sigHash = SigHash.All;
                                for (int i = 0; i < tx.Inputs.Count; i++)
                                {
                                    var input = tx.Inputs[i];

                                    if (input.PrevOut.Hash == bearer.Outpoint.Hash && input.PrevOut.N == bearer.Outpoint.N)
                                    {
                                        input = generateProperOutputSpender(input, tx, activationIn10Minutes, bearer, redeemScript, sigHash,
                                            spendingPrivateKey, selfPrivateKey, counterPartyRevokePrivateKey);
                                        break;
                                    }
                                }

                                for (int i = 0; i < tx.Inputs.Count; i++)
                                {
                                    var input = tx.Inputs[i];

                                    var inputTxId = input.PrevOut.Hash.ToString();

                                    var pregeneratedOutput = (from item in entities.PreGeneratedOutputs
                                                              where item.TransactionId == inputTxId && item.OutputNumber == input.PrevOut.N
                                                              select item).FirstOrDefault();

                                    if (pregeneratedOutput?.PrivateKey != null)
                                    {
                                        var secret = new BitcoinSecret(pregeneratedOutput.PrivateKey);
                                        var script = PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(Base58Data.GetFromBase58Data(pregeneratedOutput.Address) as BitcoinPubKeyAddress);
                                        var hash = Script.SignatureHash(new Coin(input.PrevOut.Hash, input.PrevOut.N, pregeneratedOutput.Amount, script), tx, sigHash);
                                        var signature = secret.PrivateKey.Sign(hash, sigHash);
                                        input.ScriptSig = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(signature, secret.PubKey);
                                    }
                                }

                                var verfied = builder.Verify(tx);

                                CommitmentCustomOutputSpendingTransaction response
                                    = new CommitmentCustomOutputSpendingTransaction { TransactionHex = tx.ToHex() };

                                transaction.Commit();
                                return Json(response);
                            }
                        }
                    }
                }
            }
            catch (Exception exp)
            {
                return InternalServerError(exp);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> CreateCommitmentSpendingTransactionForTimeActivatePart(string commitmentTransactionHex,
            string spendingPrivateKey, string clientPubkey, string hubPubkey, string assetName,
            string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub)
        {
            return await CreateCommitmentSpendingTransactionCore(commitmentTransactionHex, spendingPrivateKey, clientPubkey, hubPubkey, assetName,
                lockingPubkey, activationIn10Minutes, clientSendsCommitmentToHub,
                GenerateCustomScriptTimeActivateOutputSpender, null, null);
        }

        [HttpGet]
        public async Task<IHttpActionResult> AddEnoughFeesToCommitentAndBroadcast
            (string commitmentTransaction)
        {
            Transaction txToBeSent = null;
            try
            {
                var txToSend = new Transaction(commitmentTransaction);
                var fees = await GetFeeCoinsToAddToTransaction(txToSend);

                txToBeSent = txToSend;
                foreach (var item in fees)
                {
                    var txHex = await OpenAssetsHelper.GetTransactionHex
                        (item.TransactionId, WebSettings.ConnectionParams);
                    if (txHex.Item1)
                    {
                        return InternalServerError(new Exception(txHex.Item2));
                    }

                    txToBeSent.AddInput(new Transaction(txHex.Item3),
                        item.OutputNumber);

                    TransactionSignRequest feePrivateKeySignRequest = new TransactionSignRequest
                    {
                        PrivateKey = item.PrivateKey.ToString(),
                        TransactionToSign = txToBeSent.ToHex()
                    };
                    var feeSignedTransaction = await SignTransactionWorker(feePrivateKeySignRequest,
                        SigHash.All | SigHash.AnyoneCanPay);

                    txToBeSent = new Transaction(feeSignedTransaction);
                }

                var rpcClient = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential
                        (WebSettings.ConnectionParams.Username, WebSettings.ConnectionParams.Password),
                                WebSettings.ConnectionParams.IpAddress, WebSettings.ConnectionParams.BitcoinNetwork);
                await rpcClient.SendRawTransactionAsync(txToBeSent);

                return Json(new AddEnoughFeesToCommitentAndBroadcastResponse
                { TransactionId = txToBeSent.GetHash().ToString(), TransactionHex = txToBeSent.ToHex() });
            }
            catch (Exception exp)
            {
                return InternalServerError(exp);
            }
        }

        // ToDo: This should be adjusted to fee rate and transaction size
        public async Task<PreGeneratedOutput[]> GetFeeCoinsToAddToTransaction(Transaction tx)
        {
            PreGeneratedOutput feeCoin = null;
            using (SqlexpressLykkeEntities entities =
                new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                feeCoin = await GetOnePreGeneratedOutput
                    (entities, WebSettings.ConnectionParams);
            }

            return new PreGeneratedOutput[] { feeCoin };
        }
    }
}
