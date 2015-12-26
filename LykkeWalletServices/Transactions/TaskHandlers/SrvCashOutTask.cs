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
    // Sample Input: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":""}
    // Sample output: {"TransactionId":"10","Result":{"TransactionHex":"xxxxx"},"Error":null}
    public class SrvCashOutTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvCashOutTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string exchangePrivateKey) : base(network, assets, username, password, ipAddress, exchangePrivateKey)
        {
        }

        public async Task<Tuple<CashOutTaskResult, Error>> ExecuteTask(TaskToDoCashOut data)
        {
            CashOutTaskResult result = null;
            Error error = null;
            try
            {
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    var matchingAddress = await (from item in entitiesContext.KeyStorages
                                                 where item.MultiSigAddress.Equals(data.MultisigAddress)
                                                 select item).SingleOrDefaultAsync();
                    if (matchingAddress == null)
                    {
                        throw new Exception("Could not find a matching record for MultiSigAddress: "
                            + data.MultisigAddress);
                    }

                    string assetId = null;
                    string assetPrivateKey = null;
                    BitcoinAddress assetAddress = null;

                    // Getting the assetid from asset name
                    foreach (var item in Assets)
                    {
                        if (item.Name == data.Currency)
                        {
                            assetId = item.AssetId;
                            assetPrivateKey = item.PrivateKey;
                            assetAddress = (new BitcoinSecret(assetPrivateKey, Network)).PubKey.
                                GetAddress(Network);
                            break;
                        }
                    }

                    var walletOutputs = await OpenAssetsHelper.GetWalletOutputs
                        (matchingAddress.MultiSigAddress, Network);
                    if (walletOutputs.Item2)
                    {
                        error = new Error();
                        error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                        error.Message = walletOutputs.Item3;

                    }
                    else
                    {
                        var bitcoinOutputs = OpenAssetsHelper.GetWalletOutputsUncolored(walletOutputs.Item1);
                        if (!OpenAssetsHelper.IsBitcoinsEnough(bitcoinOutputs, OpenAssetsHelper.MinimumRequiredSatoshi))
                        {
                            error = new Error();
                            error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                            error.Message = "The required amount of satoshis to send transaction is " + OpenAssetsHelper.MinimumRequiredSatoshi +
                                " . The address is: " + matchingAddress.MultiSigAddress;
                        }
                        else
                        {
                            var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, assetId);
                            if (!OpenAssetsHelper.IsAssetsEnough(assetOutputs, assetId, data.Amount))
                            {
                                error = new Error();
                                error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                                error.Message = "The required amount of assets with id:" + assetId + " to send transaction is " + data.Amount +
                                    " . The address is: " + matchingAddress.MultiSigAddress;
                            }
                            else
                            {
                                var coins = (await OpenAssetsHelper.GetColoredUnColoredCoins(bitcoinOutputs, null, Network,
                                    Username, Password, IpAddress)).Item2;
                                ScriptCoin[] scriptCoins = new ScriptCoin[coins.Length];
                                for (int i = 0; i < coins.Length; i++)
                                {
                                    scriptCoins[i] = new ScriptCoin(coins[i], new Script(matchingAddress.MultiSigScript));
                                }

                                var assetCoins = (await OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, assetId, Network,
                                Username, Password, IpAddress)).Item1;

                                ColoredCoin[] assetScriptCoins = new ColoredCoin[assetCoins.Length];
                                for (int i = 0; i < assetCoins.Length; i++)
                                {
                                    assetScriptCoins[i] = new ColoredCoin(assetCoins[i].Amount,
                                        new ScriptCoin(assetCoins[i].Bearer, new Script(matchingAddress.MultiSigScript)));
                                }


                                TransactionBuilder builder = new TransactionBuilder();
                                var tx = builder
                                    .AddCoins(scriptCoins)
                                    .AddCoins(assetScriptCoins)
                                    .AddKeys(new BitcoinSecret(matchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                                    .SendAsset(assetAddress, new AssetMoney(new AssetId(new BitcoinAssetId(assetId, Network)), data.Amount))
                                    .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                                    .SetChange(new Script(matchingAddress.MultiSigScript).GetScriptAddress(Network))
                                    .BuildTransaction(true);

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

                                    result = new CashOutTaskResult
                                    {
                                        TransactionHex = tx.ToHex()
                                    };
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
