using Core;
using NBitcoin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvGetFeeOutputsStatusTask : SrvNetworkBase
    {
        // Sample input: GetFeeOutputsStatus:{"TransactionId":"10"}
        // Sample output: GetFeeOutputsStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Amount":9999.0,"Count":30},{"Amount":15000.0,"Count":1000},{"Amount":10000.0,"Count":90}]},"Error":null}
        public SrvGetFeeOutputsStatusTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        public async Task<Tuple<GetFeeOutputsStatusTaskResult, Error>> ExecuteTask
            (TaskToDoGetFeeOutputsStatus data)
        {
            GetFeeOutputsStatusTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var resultElements = from pre in entities.PreGeneratedOutputs
                                         where pre.AssetId.Equals(null)
                                         group pre by pre.Amount into g
                                         select new GetFeeOutputsStatusTaskResultElement { Amount = g.Key, Count = g.Count() };
                    result = new GetFeeOutputsStatusTaskResult
                    {
                        ResultArray = resultElements.ToArray()
                    };
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<GetFeeOutputsStatusTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGetFeeOutputsStatus data, Func<Tuple<GetFeeOutputsStatusTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
