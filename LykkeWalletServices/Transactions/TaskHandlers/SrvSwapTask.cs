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
                var wallet1Coins = await GetScriptCoinsForWallet(data.MultisigCustomer1, data.Amount1, data.Asset1);
                if (wallet1Coins.Error != null)
                {
                    error = wallet1Coins.Error;
                }
                else
                {
                    var wallet2Coins = await GetScriptCoinsForWallet(data.MultisigCustomer2, data.Amount2, data.Asset2);
                    if (wallet2Coins.Error != null)
                    {
                        error = wallet2Coins.Error;
                    }
                    else
                    {
                        // ToDo - float to long conversion, divisable curency
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

        public class GetScriptCoinsForWalletReturnType
        {
            public Error Error
            {
                get;
                set;
            }

            public ColoredCoin[] AssetScriptCoins
            {
                get;
                set;
            }

            public ScriptCoin[] ScriptCoins
            {
                get;
                set;
            }

            public KeyStorage MatchingAddress
            {
                get;
                set;
            }

            public string AssetId
            {
                get;
                set;
            }

            public long AssetMultiplicationFactor
            {
                get;
                set;
            }
        }

        private async Task<GetScriptCoinsForWalletReturnType> GetScriptCoinsForWallet
            (String multiSigAddress,
            float amount, string asset)
        {


            GetScriptCoinsForWalletReturnType ret = new GetScriptCoinsForWalletReturnType();

            try
            {
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    ret.MatchingAddress = await (from item in entitiesContext.KeyStorages
                                                 where item.MultiSigAddress.Equals(multiSigAddress)
                                                 select item).SingleOrDefaultAsync();
                }

                if (ret.MatchingAddress == null)
                {
                    throw new Exception("Could not find a matching record for MultiSigAddress: "
                        + multiSigAddress);
                }

                string assetPrivateKey = null;
                BitcoinAddress assetAddress = null;

                // Getting the assetid from asset name
                foreach (var item in Assets)
                {
                    if (item.Name == asset)
                    {
                        ret.AssetId = item.AssetId;
                        assetPrivateKey = item.PrivateKey;
                        assetAddress = (new BitcoinSecret(assetPrivateKey, Network)).PubKey.
                            GetAddress(Network);
                        ret.AssetMultiplicationFactor = item.MultiplyFactor;
                        break;
                    }
                }

                // Getting wallet outputs
                var walletOutputs = await OpenAssetsHelper.GetWalletOutputs
                    (ret.MatchingAddress.MultiSigAddress, Network);
                if (walletOutputs.Item2)
                {
                    ret.Error = new Error();
                    ret.Error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    ret.Error.Message = walletOutputs.Item3;

                }
                else
                {
                    // Getting bitcoin outputs to provide the transaction fee
                    var bitcoinOutputs = OpenAssetsHelper.GetWalletOutputsUncolored(walletOutputs.Item1);
                    if (!OpenAssetsHelper.IsBitcoinsEnough(bitcoinOutputs, OpenAssetsHelper.MinimumRequiredSatoshi))
                    {
                        ret.Error = new Error();
                        ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                        ret.Error.Message = "The required amount of satoshis to send transaction is " + OpenAssetsHelper.MinimumRequiredSatoshi +
                            " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                    }
                    else
                    {
                        // Getting the asset output to provide the assets
                        var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, ret.AssetId);
                        if (!OpenAssetsHelper.IsAssetsEnough(assetOutputs, ret.AssetId, amount, ret.AssetMultiplicationFactor))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                            ret.Error.Message = "The required amount of assets with id:" + ret.AssetId + " to send transaction is " + amount +
                                " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                        }
                        else
                        {
                            // Converting bitcoins to script coins so that we could sign the transaction
                            var coins = (await OpenAssetsHelper.GetColoredUnColoredCoins(bitcoinOutputs, null, Network,
                                Username, Password, IpAddress)).Item2;
                            ret.ScriptCoins = new ScriptCoin[coins.Length];
                            for (int i = 0; i < coins.Length; i++)
                            {
                                ret.ScriptCoins[i] = new ScriptCoin(coins[i], new Script(ret.MatchingAddress.MultiSigScript));
                            }

                            // Converting assets to script coins so that we could sign the transaction
                            var assetCoins = (await OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, ret.AssetId, Network,
                            Username, Password, IpAddress)).Item1;

                            ret.AssetScriptCoins = new ColoredCoin[assetCoins.Length];
                            for (int i = 0; i < assetCoins.Length; i++)
                            {
                                ret.AssetScriptCoins[i] = new ColoredCoin(assetCoins[i].Amount,
                                    new ScriptCoin(assetCoins[i].Bearer, new Script(ret.MatchingAddress.MultiSigScript)));
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                ret.Error = new Error();
                ret.Error.Code = ErrorCode.Exception;
                ret.Error.Message = e.ToString();
            }

            return ret;
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
