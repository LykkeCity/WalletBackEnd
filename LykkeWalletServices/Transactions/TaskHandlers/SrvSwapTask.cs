using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Request: Swap:{"TransactionId":"10", MultisigCustomer1:"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo", "Amount1":400, "Asset1":"bjkUSD", MultisigCustomer2:"2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj", "Amount2":700, "Asset2":"bjkEUR"}
    // Sample Response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxx"},"Error":null}
    public class SrvSwapTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvSwapTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string exchangePrivateKey) : base(network, assets, username, password, ipAddress, exchangePrivateKey)
        {
        }

        public async Task<Tuple<SwapTaskResult, Error>> ExecuteTask(TaskToDoSwap data)
        {
            SwapTaskResult result = null;
            Error error = null;
            try
            {
                var wallet1Coins = await OpenAssetsHelper.GetScriptCoinsForWallet(data.MultisigCustomer1, data.Amount1, data.Asset1,
                    Assets, Network, Username, Password, IpAddress);
                if (wallet1Coins.Error != null)
                {
                    error = wallet1Coins.Error;
                }
                else
                {
                    var wallet2Coins = await OpenAssetsHelper.GetScriptCoinsForWallet(data.MultisigCustomer2, data.Amount2, data.Asset2,
                        Assets, Network, Username, Password, IpAddress);
                    if (wallet2Coins.Error != null)
                    {
                        error = wallet2Coins.Error;
                    }
                    else
                    {
                        TransactionBuilder builder = new TransactionBuilder();
                        var tx = builder
                            .AddCoins(wallet1Coins.ScriptCoins)
                            .AddCoins(wallet1Coins.AssetScriptCoins)
                            .AddKeys(new BitcoinSecret(wallet1Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                            .SendAsset(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(wallet1Coins.AssetId, Network)), Convert.ToInt64((data.Amount1 * wallet1Coins.AssetMultiplicationFactor))))
                            .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi / 2))
                            .SetChange(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                            .Then()
                            .AddCoins(wallet2Coins.ScriptCoins)
                            .AddCoins(wallet2Coins.AssetScriptCoins)
                            .AddKeys(new BitcoinSecret(wallet2Coins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                            .SendAsset(new Script(wallet1Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(wallet2Coins.AssetId, Network)), Convert.ToInt64(data.Amount2 * wallet2Coins.AssetMultiplicationFactor)))
                            .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi / 2))
                            .SetChange(new Script(wallet2Coins.MatchingAddress.MultiSigScript).GetScriptAddress(Network))
                            .BuildTransaction(true);

                        Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                (tx, Username, Password, IpAddress, Network);

                        if (localerror == null)
                        {
                            result = new SwapTaskResult
                            {
                                TransactionHex = tx.ToHex()
                            };
                        }
                        else
                        {
                            error = localerror;
                        }

                        /*
                        using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                        {
                            // Checking if the inputs has been already spent
                            // ToDo - Performance should be revisted by possible join operation
                            foreach (var item in tx.Inputs)
                            {
                                string prevOut = item.PrevOut.Hash.ToString();
                                var spentTx = await (from uxto in entitiesContext.SpentOutputs
                                                     join dbTx in entitiesContext.SentTransactions on uxto.SentTransactionId equals dbTx.id
                                                     where uxto.PrevHash.Equals(prevOut) && uxto.OutputNumber.Equals(item.PrevOut.N)
                                                     select dbTx.TransactionHex).FirstOrDefaultAsync();

                                if (spentTx != null)
                                {
                                    error = new Error();
                                    error.Code = ErrorCode.PossibleDoubleSpend;
                                    error.Message = "The output number " + item.PrevOut.N + " from transaction " + item.PrevOut.Hash +
                                        " has been already spent in transcation " + spentTx;
                                    break;
                                }
                            }

                            if (error == null)
                            {
                                // First broadcating the transaction
                                RPCClient client = new RPCClient(new System.Net.NetworkCredential(Username, Password),
                                    IpAddress, Network);
                                await client.SendRawTransactionAsync(tx);

                                // Then marking the inputs as spent
                                using (var dbTransaction = entitiesContext.Database.BeginTransaction())
                                {
                                    SentTransaction dbSentTransaction = new SentTransaction
                                    {
                                        TransactionHex = tx.ToHex()
                                    };
                                    entitiesContext.SentTransactions.Add(dbSentTransaction);
                                    await entitiesContext.SaveChangesAsync();

                                    foreach (var item in tx.Inputs)
                                    {
                                        entitiesContext.SpentOutputs.Add(new SpentOutput
                                        {
                                            OutputNumber = item.PrevOut.N,
                                            PrevHash = item.PrevOut.Hash.ToString(),
                                            SentTransactionId = dbSentTransaction.id
                                        });
                                    }
                                    await entitiesContext.SaveChangesAsync();

                                    dbTransaction.Commit();
                                }

                                result = new SwapTaskResult
                                {
                                    TransactionHex = tx.ToHex()
                                };
                            }
                        }
                        */
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
