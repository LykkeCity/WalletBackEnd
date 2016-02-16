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
    // Sample request: GenerateMassOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.00015,"Count":1000, "Purpose":"fee"}
    // Sample request: GenerateMassOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.0000273,"Count":10, "Purpose":"asset:bjkUSD"}
    // Sample response: GenerateMassOutputs:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}
    public class SrvGenerateMassOutputsTask : SrvNetworkBase
    {
        private string feeAddress = null;
        private string feeAddressPrivateKey = null;
        public SrvGenerateMassOutputsTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress, string feeAddressPrivateKey) :
            base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
            this.feeAddress = feeAddress;
            this.feeAddressPrivateKey = feeAddressPrivateKey;
        }

        public async Task<Tuple<GenerateMassOutputsTaskResult, Error>> ExecuteTask(TaskToDoGenerateMassOutputs data)
        {
            GenerateMassOutputsTaskResult result = null;
            Error error = null;
            string destinationAddress = null;
            string destinationAddressPrivateKey = null;
            string assetId = null;
            bool isFeeRequested = false;
            if (data.Purpose.Trim().ToLower().Equals("fee"))
            {
                destinationAddress = feeAddress;
                destinationAddressPrivateKey = feeAddressPrivateKey;
                isFeeRequested = true;
            }
            else
            {
                var split = data.Purpose.Trim().Split(new char[] { ':' });
                if (split[0].ToLower().Equals("asset"))
                {
                    var asset = Assets.Where(a => a.Name.Equals(split[1])).Select(a => a).FirstOrDefault();
                    if (asset != null)
                    {
                        destinationAddress = asset.AssetAddress;
                        destinationAddressPrivateKey = asset.PrivateKey;
                        assetId = asset.AssetId;
                    }
                }
            }
            if (destinationAddress == null)
            {
                error = new Error();
                error.Code = ErrorCode.BadInputParameter;
                error.Message = "The specified purpose is invalid.";
            }
            else
            {
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
                        var output = uncoloredOutputs.Where(o => (o.GetValue() > minimumRequiredAmountForParticipation)).ToList();
                        if (output.Count == 0)
                        {
                            error = new Error();
                            error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                            error.Message = "There is no output available with the minimum amount of: " + minimumRequiredAmountForParticipation.ToString("n") + " satoshis.";
                        }
                        else
                        {
                            if (output.Select(o => o.GetValue()).Sum() < totalRequiredAmount)
                            {
                                error = new Error();
                                error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                                error.Message = "The sum of total applicable outputs is less than the required: " + totalRequiredAmount.ToString("n") + " satoshis.";
                            }
                            else
                            {
                                var sourceCoins = output.Select(o => new Coin(new uint256(o.GetTransactionHash()), (uint)o.GetOutputIndex(),
                                    o.GetValue(), new Script(OpenAssetsHelper.StringToByteArray(o.GetScriptHex()))));
                                TransactionBuilder builder = new TransactionBuilder();
                                //builder.DustPrevention = false;
                                builder
                                    .AddKeys(new BitcoinSecret(data.PrivateKey))
                                    .AddCoins(sourceCoins);
                                builder.SetChange(new BitcoinAddress(data.WalletAddress, Network));
                                for (int i = 0; i < data.Count; i++)
                                {
                                    builder.Send(new BitcoinAddress(destinationAddress, Network),
                                        new Money((ulong)(data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor)))
                                        .BuildTransaction(false);
                                }

                                var fee = ((ulong)builder.EstimateSize(builder.BuildTransaction(false))
                                    * OpenAssetsHelper.TransactionSendFeesInSatoshi) / 1000;
                                Transaction tx = builder.SendFees(Math.Max(fee, OpenAssetsHelper.TransactionSendFeesInSatoshi)).
                                    BuildTransaction(true);
                                IList<PreGeneratedOutput> preGeneratedOutputs = null;
                                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                                {
                                    using (var transaction = entities.Database.BeginTransaction())
                                    {
                                        Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                                    (tx, Username, Password, IpAddress, Network, entities, ConnectionString,
                                                    null, (entitiesContext) =>
                                                    {
                                                        var tId = tx.GetHash().ToString();
                                                        preGeneratedOutputs = new List<PreGeneratedOutput>();
                                                        for (int i = 0; i < tx.Outputs.Count; i++)
                                                        {
                                                            var item = tx.Outputs[i];
                                                            if (item.Value.Satoshi != (long)(data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor))
                                                            {
                                                                continue;
                                                            }
                                                            PreGeneratedOutput f = new PreGeneratedOutput();
                                                            f.TransactionId = tId;
                                                            f.OutputNumber = i;
                                                            f.Script = item.ScriptPubKey.ToHex();
                                                            f.PrivateKey = destinationAddressPrivateKey;
                                                            f.Amount = item.Value.Satoshi;
                                                            f.AssetId = assetId;
                                                            f.Address = destinationAddress;
                                                            f.Network = Network.ToString();
                                                            preGeneratedOutputs.Add(f);
                                                        }

                                                        entitiesContext.PreGeneratedOutputs.AddRange(preGeneratedOutputs);
                                                    });
                                        if (localerror == null)
                                        {
                                            result = new GenerateMassOutputsTaskResult
                                            {
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
                }
                catch (Exception e)
                {
                    error = new Error();
                    error.Code = ErrorCode.Exception;
                    error.Message = e.ToString();
                }
            }
            return new Tuple<GenerateMassOutputsTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGenerateMassOutputs data, Func<Tuple<GenerateMassOutputsTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
