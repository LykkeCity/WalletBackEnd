using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: OrdinaryCashIn:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
    // Sample Output: OrdinaryCashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx"},"Error":null}
    public class SrvOrdinaryCashInTask : SrvNetworkBase
    {
        public SrvOrdinaryCashInTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress) : base(network, assets, username, password, ipAddress)
        {
        }

        public async Task<Tuple<OrdinaryCashInTaskResult, Error>> ExecuteTask(TaskToDoOrdinaryCashIn data)
        {
            OrdinaryCashInTaskResult result = null;
            Error error = null;
            try
            {
                var walletCoins = await OpenAssetsHelper.GetOrdinaryCoinsForWallet(data.MultisigAddress, data.Amount, data.Currency,
                    Assets, Network, Username, Password, IpAddress);
                if (walletCoins.Error != null)
                {
                    error = walletCoins.Error;
                }
                else
                {
                    TransactionBuilder builder = new TransactionBuilder();
                    var tx = builder
                        .AddCoins(walletCoins.Coins)
                        .AddCoins(walletCoins.AssetCoins)
                        .AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey))
                        .SendAsset(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.AssetMultiplicationFactor))))
                        .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                        .SetChange(new BitcoinAddress(walletCoins.MatchingAddress.WalletAddress))
                        .BuildTransaction(true);

                    Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                        (tx, Username, Password, IpAddress, Network);

                    if (localerror == null)
                    {
                        result = new OrdinaryCashInTaskResult
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
