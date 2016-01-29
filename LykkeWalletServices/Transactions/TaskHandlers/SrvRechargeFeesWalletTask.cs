using Core;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Tne service used to generate fees outptut, which is used to create transaction
    // Sample request: RechargeFeesWallet:{"WalletAddress":"mtgbKeNYngWvjmUaSfqhnKD3s2niC3tsCx","PrivateKey":"???","FeeAmount":0.00015,"Count":1000}
    // Sample response: RechargeFeesWallet:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}
    public class SrvRechargeFeesWalletTask : SrvNetworkBase
    {
        private string feeAddress = null;
        public SrvRechargeFeesWalletTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : 
            base(network, assets, username, password, ipAddress, connectionString)
        {
            this.feeAddress = feeAddress;
        }

        public async Task<Tuple<RechargeFeesWalletTaskResult, Error>> ExecuteTask(TaskToDoRechargeFeesWallet data)
        {
            RechargeFeesWalletTaskResult result = null;
            Error error = null;
            try
            {
                var outputs = await OpenAssetsHelper.GetWalletOutputs(data.WalletAddress, Network);
                if (outputs.Item2)
                {
                    error = new Error();
                    error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    error.Message = outputs.Item3;
                }
                else
                {
                    var uncoloredOutputs = OpenAssetsHelper.GetWalletOutputsUncolored(outputs.Item1);
                    float totalRequiredAmount = data.Count * data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor; // Convert to satoshi
                    float minimumRequiredAmountForParticipation = (ulong)(0.001 * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor);
                    var output = uncoloredOutputs.Where(o => (o.value > minimumRequiredAmountForParticipation)).ToList();
                    if (output.Count == 0)
                    {
                        error = new Error();
                        error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                        error.Message = "There is no output available with the minimum amount of: " + minimumRequiredAmountForParticipation.ToString("n") + " satoshis.";
                    }
                    else
                    {
                        if (output.Select(o => o.value).Sum() < totalRequiredAmount)
                        {
                            error = new Error();
                            error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                            error.Message = "The sum of total applicable outputs is less than the required: " + totalRequiredAmount.ToString("n") + " satoshis.";
                        }
                        else
                        {
                            var sourceCoins = output.Select(o => new Coin(new uint256(o.transaction_hash), (uint)o.output_index,
                                o.value, new Script(OpenAssetsHelper.StringToByteArray(o.script_hex))));
                            TransactionBuilder builder = new TransactionBuilder();
                            builder
                                .AddKeys(new BitcoinSecret(data.PrivateKey))
                                .AddCoins(sourceCoins);
                            builder.SetChange(new BitcoinAddress(data.WalletAddress, Network));
                            for (int i = 0; i < data.Count; i++)
                            {
                                builder.Send(new BitcoinAddress(feeAddress, Network),
                                    new Money((ulong)(data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor)))
                                    .BuildTransaction(false);
                            }

                            var fee = ((ulong)builder.EstimateSize(builder.BuildTransaction(false))
                                * OpenAssetsHelper.TransactionSendFeesInSatoshi) / 1000;
                            Transaction tx = builder.SendFees(fee).BuildTransaction(true);
                            IList<FeeOutput> feeOutputs = null;
                            Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, Username, Password, IpAddress, Network, ConnectionString, 
                                        () => {
                                            var tId = tx.ToHex();
                                            feeOutputs = new List<FeeOutput>();
                                            for (int i = 0; i < tx.Outputs.Count; i++)
                                            {
                                                var item = tx.Outputs[i];
                                                if (item.Value.Satoshi != (long)(data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor))
                                                {
                                                    continue;
                                                }
                                                FeeOutput f = new FeeOutput();
                                                f.TransactionId = tId;
                                                f.OutputNumber = i;
                                                f.Script = item.ScriptPubKey.ToHex();
                                                f.PrivateKey = data.PrivateKey;
                                                f.Amount = item.Value.Satoshi;
                                                feeOutputs.Add(f);
                                            }
                                        }, async (entitiesContext) => {
                                            entitiesContext.FeeOutputs.AddRange(feeOutputs);
                                            await entitiesContext.SaveChangesAsync();
                                        });
                            if (localerror == null)
                            {
                                result = new RechargeFeesWalletTaskResult
                                {
                                    TransactionHash = tx.GetHash().ToString()
                                };
                            }
                            else
                            {
                                error = localerror;
                            }
                            /*
                            using (SqliteLykkeServicesEntities entitiesContext =
                                new SqliteLykkeServicesEntities(ConnectionString))
                            {
                                var tId = tx.ToHex();
                                IList<FeeOutput> feeOutpus = new List<FeeOutput>();
                                for (int i = 0; i < tx.Outputs.Count; i++)
                                {
                                    var item = tx.Outputs[i];
                                    FeeOutput f = new FeeOutput();
                                    f.TransactionId = tId;
                                    f.OutputNumber = i;
                                    f.Script = item.ScriptPubKey.ToHex();
                                    f.PrivateKey = data.PrivateKey;
                                    f.Amount = item.Value.Satoshi;
                                }
                                using (var dbTransaction = entitiesContext.Database.BeginTransaction())
                                {
                                    entitiesContext.FeeOutputs.AddRange(feeOutpus);
                                    entitiesContext.SaveChanges();

                                    Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, Username, Password, IpAddress, Network, ConnectionString);
                                    if (localerror == null)
                                    {
                                        dbTransaction.Commit();

                                        result = new RechargeFeesWalletTaskResult
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
                            */
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
            return new Tuple<RechargeFeesWalletTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoRechargeFeesWallet data, Func<Tuple<RechargeFeesWalletTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
