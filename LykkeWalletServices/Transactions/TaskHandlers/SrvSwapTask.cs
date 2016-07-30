using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Request: Swap:{"TransactionId":"10", MultisigCustomer1:"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo", "Amount1":400, "Asset1":"bjkUSD", MultisigCustomer2:"2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj", "Amount2":700, "Asset2":"bjkEUR"}
    // Sample Response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvSwapTask : SrvNetworkInvolvingExchangeBase
    {
        private static int swapMinimumConfirmationNumber = 0;

        public static int SwapMinimumConfirmationNumber
        {
            get
            {
                return swapMinimumConfirmationNumber;
            }
            set
            {
                swapMinimumConfirmationNumber = value;
            }
        }

        public SrvSwapTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        // in order to pay from each address separately we can build two transactions for each
        // source address and then calculate the fee for each transaction, the build the third final transaction
        public async Task<Tuple<SwapTaskResult, Error>> ExecuteTask(TaskToDoSwap data)
        {
            SwapTaskResult result = null;
            Error error = null;

            Func<int> getMinimumConfirmationNumber = (() => { return SwapMinimumConfirmationNumber; });

            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    OpenAssetsHelper.GetScriptCoinsForWalletReturnType wallet1Coins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigCustomer1, !OpenAssetsHelper.IsRealAsset(data.Asset1) ? Convert.ToInt64(data.Amount1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount1, data.Asset1,
                    Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false, true, getMinimumConfirmationNumber);
                    if (wallet1Coins.Error != null)
                    {
                        error = wallet1Coins.Error;
                    }
                    else
                    {
                        OpenAssetsHelper.GetScriptCoinsForWalletReturnType wallet2Coins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigCustomer2, !OpenAssetsHelper.IsRealAsset(data.Asset2) ? Convert.ToInt64(data.Amount2 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount2, data.Asset2,
                         Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false, true, getMinimumConfirmationNumber);
                        if (wallet2Coins.Error != null)
                        {
                            error = wallet2Coins.Error;
                        }
                        else
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                long coloredCoinCount1 = 0;
                                long coloredCoinCount2 = 0;

                                BitcoinSecret[] secret = null;
                                ScriptCoin[] uncoloredCoins = null;
                                ColoredCoin[] coloredCoins = null;
                                BitcoinScriptAddress destAddress = null;
                                BitcoinScriptAddress changeAddress = null;
                                AssetMoney coloredAmount = null;
                                long uncoloredAmount = 0;
                                string asset = null;

                                secret = new BitcoinSecret[] { new BitcoinSecret(wallet1Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey) };
                                uncoloredCoins = wallet1Coins?.ScriptCoins;
                                coloredCoins = wallet1Coins?.AssetScriptCoins;
                                destAddress = new Script(wallet2Coins?.MatchingAddress?.MultiSigScript).GetScriptAddress(Network);
                                changeAddress = new Script(wallet1Coins?.MatchingAddress?.MultiSigScript).GetScriptAddress(Network);
                                coloredAmount = (wallet1Coins?.Asset?.AssetId == null) ? null : new AssetMoney(new AssetId(new BitcoinAssetId(wallet1Coins?.Asset?.AssetId, Network)), Convert.ToInt64((data.Amount1 * (wallet1Coins?.Asset?.AssetMultiplicationFactor ?? 0))));
                                uncoloredAmount = Convert.ToInt64(data.Amount1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);
                                asset = data?.Asset1;

                                TransactionBuilder builder = new TransactionBuilder();
                                builder.BuildHalfOfSwap(secret, uncoloredCoins, coloredCoins, destAddress, changeAddress, coloredAmount,
                                    uncoloredAmount, asset, out coloredCoinCount1);

                                secret = new BitcoinSecret[] { new BitcoinSecret(wallet2Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey) };
                                uncoloredCoins = wallet2Coins?.ScriptCoins;
                                coloredCoins = wallet2Coins?.AssetScriptCoins;
                                destAddress = new Script(wallet1Coins?.MatchingAddress?.MultiSigScript).GetScriptAddress(Network);
                                changeAddress = new Script(wallet2Coins?.MatchingAddress?.MultiSigScript).GetScriptAddress(Network);
                                coloredAmount = (wallet2Coins?.Asset?.AssetId == null) ? null : new AssetMoney(new AssetId(new BitcoinAssetId(wallet2Coins?.Asset?.AssetId, Network)), Convert.ToInt64((data.Amount2 * (wallet2Coins?.Asset?.AssetMultiplicationFactor ?? 0))));
                                uncoloredAmount = Convert.ToInt64(data.Amount2 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);
                                asset = data?.Asset2;
                                builder.BuildHalfOfSwap(secret, uncoloredCoins, coloredCoins, destAddress, changeAddress, coloredAmount,
                                    uncoloredAmount, asset, out coloredCoinCount2);

                                var tx = (await builder.AddEnoughPaymentFee(entities, new RPCConnectionParams { Username = Username, Password = Password, Network = Network.ToString(), IpAddress = IpAddress },
                                    FeeAddress, coloredCoinCount1 + coloredCoinCount2)).BuildTransaction(true);

                                var txHash = tx.GetHash().ToString();

                                OpenAssetsHelper.LykkeJobsNotificationMessage lykkeJobsNotificationMessage =
                                    new OpenAssetsHelper.LykkeJobsNotificationMessage();
                                lykkeJobsNotificationMessage.Operation = "Swap";
                                lykkeJobsNotificationMessage.TransactionId = data.TransactionId;
                                lykkeJobsNotificationMessage.BlockchainHash = txHash;

                                Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, Username, Password, IpAddress, Network, entities, ConnectionString, lykkeJobsNotificationMessage)).Error;

                                if (localerror == null)
                                {
                                    result = new SwapTaskResult
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
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<SwapTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoSwap data, Func<Tuple<SwapTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
