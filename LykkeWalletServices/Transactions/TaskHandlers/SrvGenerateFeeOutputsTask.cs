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
    // Sample request: GenerateFeeOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.00015,"Count":1000}
    // Sample response: GenerateFeeOutputs:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}
    public class SrvGenerateFeeOutputsTask : SrvNetworkBase
    {
        private string feeAddress = null;
        private string feeAddressPrivateKey = null;
        public SrvGenerateFeeOutputsTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress, string feeAddressPrivateKey) :
            base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
            this.feeAddress = feeAddress;
            this.feeAddressPrivateKey = feeAddressPrivateKey;
        }

        

        public async Task<Tuple<GenerateMassOutputsTaskResult, Error>> ExecuteTask(TaskToDoGenerateFeeOutputs data)
        {
            return await OpenAssetsHelper.GenerateMassOutputs(data, "fee", Username, Password, IpAddress,
                Network, ConnectionString, Assets, feeAddress, feeAddressPrivateKey);
        }

        public void Execute(TaskToDoGenerateFeeOutputs data, Func<Tuple<GenerateMassOutputsTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
