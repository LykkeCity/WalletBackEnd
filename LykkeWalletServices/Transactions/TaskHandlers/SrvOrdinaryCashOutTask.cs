using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: OrdinaryCashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
    // Sample Output: OrdinaryCashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx"},"Error":null}
    public class SrvOrdinaryCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvOrdinaryCashOutTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string exchangePrivateKey) : base(network, assets, username, password, ipAddress, exchangePrivateKey)
        {
        }

        public async Task<Tuple<OrdinaryCashOutTaskResult, Error>> ExecuteTask(TaskToDoOrdinaryCashOut data)
        {
            OrdinaryCashOutTaskResult result = null;
            Error error = null;
            try
            {
                var walletCoins = await OpenAssetsHelper.GetScriptCoinsForWallet(data.MultisigAddress, data.Amount, data.Currency,
                    Assets, Network, Username, Password, IpAddress);
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
                        .SendAsset(new BitcoinAddress(walletCoins.MatchingAddress.WalletAddress), new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.AssetMultiplicationFactor))))
                        .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                        .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                        .BuildTransaction(true);

                    Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                        (tx, Username, Password, IpAddress, Network);

                    if (localerror == null)
                    {
                        result = new OrdinaryCashOutTaskResult
                        {
                            TransactionHex = tx.ToHex()
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
