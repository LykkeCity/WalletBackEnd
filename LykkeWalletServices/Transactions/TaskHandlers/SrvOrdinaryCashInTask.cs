using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using System.Linq;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: OrdinaryCashIn:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
    // Sample Output: OrdinaryCashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvOrdinaryCashInTask : SrvNetworkBase
    {
        public SrvOrdinaryCashInTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        public async Task<Tuple<OrdinaryCashInTaskResult, Error>> ExecuteTask(TaskToDoOrdinaryCashIn data)
        {
            OrdinaryCashInTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var MultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities);
                    OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType walletCoins =
                        (OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(MultisigAddress.WalletAddress, !OpenAssetsHelper.IsRealAsset(data.Currency) ? Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount, data.Currency,
                         Assets, connectionParams, ConnectionString, entities, true, false);
                    if (walletCoins.Error != null)
                    {
                        error = walletCoins.Error;
                    }
                    else
                    {
                        using (var transaction = entities.Database.BeginTransaction())
                        {
                            TransactionBuilder builder = new TransactionBuilder();
                            builder
                                .SetChange(BitcoinAddress.Create(MultisigAddress.WalletAddress), ChangeType.Colored)
                                .AddKeys(new BitcoinSecret(MultisigAddress.WalletPrivateKey));
                            if (OpenAssetsHelper.IsRealAsset(data.Currency))
                            {
                                builder = builder.AddCoins(walletCoins.AssetCoins)
                                    .SendAsset(new Script(MultisigAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork), new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))));
                                builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                    FeeAddress, 2));
                            }
                            else
                            {
                                builder.AddCoins(walletCoins.Coins);
                                builder.SendWithChange(new Script(MultisigAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork),
                                    Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor),
                                    walletCoins.Coins,
                                    BitcoinAddress.Create(MultisigAddress.WalletAddress));
                                builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                    FeeAddress, 0));
                            }

                            var tx = builder
                                .BuildTransaction(true, SigHash.All);

                            var txHash = tx.GetHash().ToString();

                            OpenAssetsHelper.LykkeJobsNotificationMessage lykkeJobsNotificationMessage =
                                new OpenAssetsHelper.LykkeJobsNotificationMessage();
                            lykkeJobsNotificationMessage.Operation = "OrdinaryCashIn";
                            lykkeJobsNotificationMessage.TransactionId = data.TransactionId;
                            lykkeJobsNotificationMessage.BlockchainHash = txHash;

                            Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                (tx, connectionParams, entities, ConnectionString, lykkeJobsNotificationMessage)).Error;

                            if (localerror == null)
                            {
                                result = new OrdinaryCashInTaskResult
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
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<OrdinaryCashInTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoOrdinaryCashIn data, Func<Tuple<OrdinaryCashInTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
