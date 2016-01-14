using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":""}
    // Sample output: CashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvCashOutTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<CashOutTaskResult, Error>> ExecuteTask(TaskToDoCashOut data)
        {
            CashOutTaskResult result = null;
            Error error = null;
            try
            {
                //var walletCoins = await OpenAssetsHelper.GetScriptCoinsForWallet(data.MultisigAddress, data.Amount, data.Currency,
                //    Assets, Network, Username, Password, IpAddress);
                OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType) await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress, data.Amount, data.Currency,
                    Assets, Network, Username, Password, IpAddress, ConnectionString, false);
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
                        .SendAsset(walletCoins.Asset.AssetAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                        .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                        .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                        .BuildTransaction(true);

                    Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                        (tx, Username, Password, IpAddress, Network, ConnectionString);

                    if (localerror == null)
                    {
                        result = new CashOutTaskResult
                        {
                            TransactionHex = tx.ToHex(),
                            TransactionHash = tx.GetHash().ToString()
                        };
                    }
                    else
                    {
                        error = localerror;
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
