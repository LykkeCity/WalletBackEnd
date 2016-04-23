using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Request: Swap:{"TransactionId":"10", MultisigCustomer1:"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo", "Amount1":400, "Asset1":"bjkUSD", MultisigCustomer2:"2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj", "Amount2":700, "Asset2":"bjkEUR"}
    // Sample Response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvSwapTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvSwapTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<SwapTaskResult, Error>> ExecuteTask(TaskToDoSwap data)
        {
            SwapTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    OpenAssetsHelper.GetScriptCoinsForWalletReturnType wallet1Coins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigCustomer1, !OpenAssetsHelper.IsRealAsset(data.Asset1) ? Convert.ToInt64(data.Amount1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount1, data.Asset1,
                    Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false);
                    if (wallet1Coins.Error != null)
                    {
                        error = wallet1Coins.Error;
                    }
                    else
                    {
                        OpenAssetsHelper.GetScriptCoinsForWalletReturnType wallet2Coins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigCustomer2, !OpenAssetsHelper.IsRealAsset(data.Asset2) ? Convert.ToInt64(data.Amount2 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor) : 0, data.Amount2, data.Asset2,
                         Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false);
                        if (wallet2Coins.Error != null)
                        {
                            error = wallet2Coins.Error;
                        }
                        else
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                TransactionBuilder builder = new TransactionBuilder();
                                builder.AddKeys(new BitcoinSecret(wallet1Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey));
                                if (OpenAssetsHelper.IsRealAsset(data.Asset1))
                                {
                                    builder.AddCoins(wallet1Coins.AssetScriptCoins);
                                    builder = await builder.AddEnoughPaymentFee(entities, Network.ToString());
                                    builder.SendAsset(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(wallet1Coins.Asset.AssetId, Network)), Convert.ToInt64((data.Amount1 * wallet1Coins.Asset.AssetMultiplicationFactor))));
                                }
                                else
                                {
                                    builder.AddCoins(wallet1Coins.ScriptCoins);
                                    builder.Send(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network),
                                    Convert.ToInt64(data.Amount1 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor))
                                    .SetChange(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network)).Then();
                                    builder = await builder.AddEnoughPaymentFee(entities, Network.ToString());
                                }
                                builder.SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi / 2))
                                .SetChange(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                                .Then();
                                builder.AddKeys(new BitcoinSecret(wallet2Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey));
                                if (OpenAssetsHelper.IsRealAsset(data.Asset2))
                                {
                                    builder.AddCoins(wallet2Coins.AssetScriptCoins);
                                    builder = await builder.AddEnoughPaymentFee(entities, Network.ToString());
                                    builder.SendAsset(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(wallet2Coins.Asset.AssetId, Network)), Convert.ToInt64(data.Amount2 * wallet2Coins.Asset.AssetMultiplicationFactor)));
                                }
                                else
                                {
                                    builder.AddCoins(wallet2Coins.ScriptCoins);
                                    builder.Send(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network),
                                    Convert.ToInt64(data.Amount2 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor))
                                    .SetChange(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network)).Then();
                                    builder = await builder.AddEnoughPaymentFee(entities, Network.ToString());
                                }
                                builder.SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi / 2))
                                .SetChange(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network));

                                var tx = builder.BuildTransaction(true);

                                Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, Username, Password, IpAddress, Network, entities, ConnectionString);

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
