using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":""}
    // Sample output: CashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvCashOutTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<CashOutTaskResult, Error>> ExecuteTask(TaskToDoCashOut data)
        {
            CashOutTaskResult result = null;
            Error error = null;

            try
            {
                if (!OpenAssetsHelper.IsRealAsset(data.Currency))
                {
                    error = new Error();
                    error.Code = ErrorCode.AssetNotFound;
                    error.Message = "Real asset should be requested.";
                }
                else
                {
                    using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                    {
                        OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress, 0, data.Amount, data.Currency,
                            Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false);
                        if (walletCoins.Error != null)
                        {
                            error = walletCoins.Error;
                        }
                        else
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                TransactionBuilder builder = new TransactionBuilder();
                                var tx = (await builder
                                    .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), ChangeType.Colored)
                                    .AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                                    .AddCoins(walletCoins.AssetScriptCoins)
                                    .SendAsset(walletCoins.Asset.AssetAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                                    .AddEnoughPaymentFee(entities, new RPCConnectionParams { Username = Username, Password = Password, Network = Network.ToString(), IpAddress = IpAddress },
                                    FeeAddress, 2))
                                    .BuildTransaction(true);

                                var txHash = tx.GetHash().ToString();

                                OpenAssetsHelper.LykkeJobsNotificationMessage lykkeJobsNotificationMessage =
                                    new OpenAssetsHelper.LykkeJobsNotificationMessage();
                                lykkeJobsNotificationMessage.Operation = "CashOut";
                                lykkeJobsNotificationMessage.TransactionId = data.TransactionId;
                                lykkeJobsNotificationMessage.BlockchainHash = txHash;

                                Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                    (tx, Username, Password, IpAddress, Network, entities, ConnectionString, lykkeJobsNotificationMessage);

                                if (localerror == null)
                                {
                                    result = new CashOutTaskResult
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
            return new Tuple<CashOutTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoCashOut data, Func<Tuple<CashOutTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
