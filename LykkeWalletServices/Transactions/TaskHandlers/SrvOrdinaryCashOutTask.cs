using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using System.Linq;
using Core.LykkeIntegration;
using Core.LykkeIntegration.Services;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: OrdinaryCashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
    // Sample Output: OrdinaryCashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvOrdinaryCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public SrvOrdinaryCashOutTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string connectionString, IPreBroadcastHandler preBroadcastHandler) : 
                base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
            _preBroadcastHandler = preBroadcastHandler;
        }

        public async Task<Tuple<OrdinaryCashOutTaskResult, Error>> ExecuteTask(TaskToDoOrdinaryCashOut data)
        {
            OrdinaryCashOutTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)
                        await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress, !OpenAssetsHelper.IsRealAsset(data.Currency) ? Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount, data.Currency,
                        Assets, connectionParams, ConnectionString, entities, false);
                    if (walletCoins.Error != null)
                    {
                        error = walletCoins.Error;
                    }
                    else
                    {
                        var dest = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.PublicWallet);
                        if (dest == null)
                        {
                            error = new Error();
                            error.Code = ErrorCode.InvalidAddress;
                            error.Message = "Invalid address provided";
                        }
                        else
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                TransactionBuilder builder = new TransactionBuilder();
                                builder
                                    .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork), ChangeType.Colored)
                                    .AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey), (new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey)).PubKey.GetExchangePrivateKey());
                                if (OpenAssetsHelper.IsRealAsset(data.Currency))
                                {
                                    builder.AddCoins(walletCoins.AssetScriptCoins).
                                        SendAsset(dest, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))));
                                    builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                        FeeAddress, 2));
                                }
                                else
                                {
                                    builder.AddCoins(walletCoins.ScriptCoins);
                                    builder.SendWithChange(dest,
                                        Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor),
                                        walletCoins.ScriptCoins,
                                        new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork));
                                    builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                        FeeAddress, 0));
                                }

                                var tx = builder.BuildTransaction(true);

                                var txHash = tx.GetHash().ToString();

                                var handledTxRequest = new HandleTxRequest
                                {
                                    Operation = "OrdinaryCashOut",
                                    TransactionId = data.TransactionId,
                                    BlockchainHash = txHash
                                };

                                Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                    (tx, connectionParams, entities, ConnectionString, handledTxRequest, _preBroadcastHandler)).Error;

                                if (localerror == null)
                                {
                                    result = new OrdinaryCashOutTaskResult
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
                    }
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<OrdinaryCashOutTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoOrdinaryCashOut data, Func<Tuple<OrdinaryCashOutTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
