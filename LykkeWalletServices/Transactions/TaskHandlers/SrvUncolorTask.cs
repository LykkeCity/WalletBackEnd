using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using System.Linq;
using Core.LykkeIntegration;
using Core.LykkeIntegration.Services;
using static LykkeWalletServices.OpenAssetsHelper;
using System.Collections.Generic;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: Uncolor:{"TransactionId":"10","MultisigAddress":"2N8Uvcw6NmJKndpJw1V2qEghSHUvbrjcDPL","Amount":3,"Currency":"TestExchangeUSD"}
    // Sample Output: Uncolor:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvUncolorTask : SrvNetworkInvolvingExchangeBase
    {
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public SrvUncolorTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string connectionString, IPreBroadcastHandler preBroadcastHandler) :
                base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
            _preBroadcastHandler = preBroadcastHandler;
        }

        public async Task<Tuple<UncolorTaskResult, Error>> ExecuteTask(TaskToDoUncolor data)
        {
            UncolorTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)
                        await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress, !OpenAssetsHelper.IsRealAsset(data.Currency) ? Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount, data.Currency,
                        Assets, connectionParams, ConnectionString, entities, false, true, null, data.IgnoreUnconfirmed);
                    if (walletCoins.Error != null)
                    {
                        error = walletCoins.Error;
                    }
                    else
                    {
                        if (OpenAssetsHelper.IsRealAsset(data.Currency))
                        {
                            var dest = Base58Data.GetFromBase58Data(data.MultisigAddress) as BitcoinAddress;

                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                TransactionBuilder builder = new TransactionBuilder();
                                var exchangePrivateKey = (new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey)).PubKey.GetExchangePrivateKey(entities);
                                builder
                                    .SetChange(dest, ChangeType.Colored);
                                //.AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey), exchangePrivateKey);

                                builder.AddCoins(walletCoins.AssetScriptCoins).
                                    SendAsset(dest, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))));
                                builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                    FeeAddress, 2));

                                var tx = builder.BuildTransaction(true);

                                var colorMarker = tx.GetColoredMarker();
                                var quantities = colorMarker.Quantities;

                                bool found = false;
                                for (int i = 0; i < quantities.Length; i++)
                                {
                                    if (quantities[i] == Convert.ToUInt64(data.Amount * walletCoins.Asset.AssetMultiplicationFactor))
                                    {
                                        quantities[i] = 0;
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    throw new Exception("Could not build the proper transaction to uncolor.");
                                }

                                colorMarker.Quantities = quantities;

                                for (int i = 0; i < tx.Outputs.Count(); i++)
                                {
                                    if (tx.Outputs[i].Value == 0)
                                    {
                                        tx.Outputs[i].ScriptPubKey = colorMarker.GetScript();
                                        break;
                                    }
                                }

                                IList<string> feePrivateKeyList = new List<string>();
                                IList<string> feePrivateKeyListDistinct = new List<string>();
                                string inputHash = null;

                                foreach (var input in tx.Inputs)
                                {
                                    inputHash = input.PrevOut.Hash.ToString();
                                    var feeInput = (from dbInput in entities.PreGeneratedOutputs
                                                    where dbInput.TransactionId == inputHash && dbInput.OutputNumber == input.PrevOut.N
                                                    select dbInput.PrivateKey).FirstOrDefault();

                                    if (feeInput != null)
                                    {
                                        feePrivateKeyList.Add(feeInput);
                                    }
                                }
                                feePrivateKeyListDistinct = feePrivateKeyList.Distinct().ToList();

                                TransactionSignRequest request = null;
                                foreach (var feePrivateKey in feePrivateKeyListDistinct)
                                {
                                    request = new TransactionSignRequest { PrivateKey = feePrivateKey, TransactionToSign = tx.ToHex() };
                                    tx = new Transaction(await SignTransactionWorker(request));
                                }

                                request =
                                    new TransactionSignRequest { PrivateKey = walletCoins.MatchingAddress.WalletPrivateKey, TransactionToSign = tx.ToHex() };
                                tx = new Transaction(await SignTransactionWorker(request));

                                request =
                                    new TransactionSignRequest { PrivateKey = exchangePrivateKey.ToString(), TransactionToSign = tx.ToHex() };
                                tx = new Transaction(await SignTransactionWorker(request));

                                var txHash = tx.GetHash().ToString();

                                var handledTxRequest = new HandleTxRequest
                                {
                                    Operation = "Uncolor",
                                    TransactionId = data.TransactionId,
                                    BlockchainHash = txHash
                                };

                                Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                    (tx, connectionParams, entities, ConnectionString, handledTxRequest, _preBroadcastHandler)).Error;

                                if (localerror == null)
                                {
                                    result = new UncolorTaskResult
                                    {
                                        TransactionHex = tx.ToHex(),
                                        TransactionHash = txHash
                                    };
                                }
                                else
                                {
                                    error = localerror;
                                }

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
                        else
                        {
                            error = new Error();
                            error.Code = ErrorCode.OperationNotSupported;
                            error.Message = string.Format("Could not uncolor the provided asset {0}", data.Currency);
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
            return new Tuple<UncolorTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoUncolor data, Func<Tuple<UncolorTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
