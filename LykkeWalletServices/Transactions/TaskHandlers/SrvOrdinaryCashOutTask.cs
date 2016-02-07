using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: OrdinaryCashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
    // Sample Output: OrdinaryCashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvOrdinaryCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvOrdinaryCashOutTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, exchangePrivateKey, connectionString)
        {
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
                        await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress, data.Amount, data.Currency,
                        Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false);
                    if (walletCoins.Error != null)
                    {
                        error = walletCoins.Error;
                    }
                    else
                    {
                        TransactionBuilder builder = new TransactionBuilder();
                        var tx = builder
                            .AddCoins(walletCoins.ScriptCoins)
                            .AddCoins(walletCoins.AssetScriptCoins)
                            .AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                            .SendAsset(new BitcoinAddress(walletCoins.MatchingAddress.WalletAddress), new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                            .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                            .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                            .BuildTransaction(true);

                        using (var transaction = entities.Database.BeginTransaction())
                        {
                            Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                (tx, Username, Password, IpAddress, Network, entities, ConnectionString);

                            if (localerror == null)
                            {
                                result = new OrdinaryCashOutTaskResult
                                {
                                    TransactionHex = tx.ToHex(),
                                    TransactionHash = tx.GetHash().ToString()
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
