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
    // Sample request: GenerateIssuerOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.0000273,"Count":10, "AssetName":"bjkUSD"}
    // Sample response: GenerateIssuerOutputs:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}
    public class SrvGenerateIssuerOutputsTask : SrvNetworkBase
    {
        public SrvGenerateIssuerOutputsTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString) :
            base(network, assets, username, password, ipAddress, connectionString, null)
        {
        }

        public async Task<Tuple<GenerateMassOutputsTaskResult, Error>> ExecuteTask(TaskToDoGenerateIssuerOutputs data)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
            {
                return await OpenAssetsHelper.GenerateMassOutputs(data, "asset:" + data.AssetName, connectionParams,
                    ConnectionString, Assets, null, null);
            }
        }

        public void Execute(TaskToDoGenerateIssuerOutputs data, Func<Tuple<GenerateMassOutputsTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
