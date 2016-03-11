using Core;
using NBitcoin;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvGetIssuersOutputStatusTask : SrvNetworkBase
    {
        // Sample input: GetIssuersOutputStatus:{"TransactionId":"10"}
        // Sample output: GetIssuersOutputStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkEUR","Amount":15000.0,"Count":1000},{"Asset":"bjkUSD","Amount":2730.0,"Count":30},{"Asset":"bjkUSD","Amount":15000.0,"Count":1000}]},"Error":null}
        public SrvGetIssuersOutputStatusTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        public async Task<Tuple<GetIssuersOutputStatusTaskResult, Error>> ExecuteTask
            (TaskToDoGetIssuersOutputStatus data)
        {
            GetIssuersOutputStatusTaskResult result = null;
            IList<GetIssuersOutputStatusTaskResultElement> elements =
                new List<GetIssuersOutputStatusTaskResultElement>();
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var nestedGroups =
                        from pregeneratedOutput in entities.PreGeneratedOutputs
                        where !pregeneratedOutput.AssetId.Equals(null)
                        group pregeneratedOutput by pregeneratedOutput.AssetId into newGroup1
                        from newGroup2 in
                        (from pregeneratedOutput in newGroup1
                         group pregeneratedOutput by pregeneratedOutput.Amount)
                        group newGroup2 by newGroup1.Key;

                    foreach(var outerGroup in nestedGroups)
                    {
                        foreach(var innerGroup in outerGroup)
                        {
                            GetIssuersOutputStatusTaskResultElement element 
                                = new GetIssuersOutputStatusTaskResultElement();
                            element.Asset = (from asset in Assets
                                             where asset.AssetId.Equals(outerGroup.Key) select asset.Name).FirstOrDefault();
                            element.Amount = innerGroup.Key;
                            element.Count = innerGroup.Count();

                            elements.Add(element);
                        }
                    }
                    result = new GetIssuersOutputStatusTaskResult
                    {
                        ResultArray = elements.ToArray()
                    };
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<GetIssuersOutputStatusTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGetIssuersOutputStatus data, Func<Tuple<GetIssuersOutputStatusTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
