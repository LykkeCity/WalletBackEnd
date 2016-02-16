using Core;
using NBitcoin;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: GetCurrentBalance:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B" }
    // Sample response: {"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkUSD","Amount":9400.0},{"Asset":"bjkEUR","Amount":1300.0},{"Asset":"TestExchangeUSD","Amount":1300.0}]},"Error":null}
    public class SrvGetCurrentBalanceTask : SrvNetworkBase
    {
        public SrvGetCurrentBalanceTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        public async Task<Tuple<GetCurrentBalanceTaskResult, Error>> ExecuteTask
            (TaskToDoGetCurrentBalance data)
        {
            GetCurrentBalanceTaskResult result = null;
            IList<GetCurrentBalanceTaskResultElement> resultElements
                = new List<GetCurrentBalanceTaskResultElement>();
            Error error = null;
            try
            {
                var walletOuputs = await OpenAssetsHelper.GetWalletOutputs(data.MultisigAddress, Network);
                if (walletOuputs.Item2)
                {
                    error = new Error();
                    error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    error.Message = walletOuputs.Item3;
                }
                else
                {
                    foreach (var item in Assets)
                    {
                        float tempValue = OpenAssetsHelper.GetAssetBalance(walletOuputs.Item1, item.AssetId, item.MultiplyFactor);
                        GetCurrentBalanceTaskResultElement element = new GetCurrentBalanceTaskResultElement();
                        element.Asset = item.Name;
                        element.Amount = tempValue;
                        resultElements.Add(element);
                    }

                    result = new GetCurrentBalanceTaskResult
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
            return new Tuple<GetCurrentBalanceTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGetCurrentBalance data, Func<Tuple<GetCurrentBalanceTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
