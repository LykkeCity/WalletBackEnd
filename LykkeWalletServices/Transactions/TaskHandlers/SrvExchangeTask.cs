using System;
using System.Threading.Tasks;
using Core;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvExchangeTaskHandler
    {

        public async Task<bool> ExecuteTask(TaskToDoSendAsset data)
        {
            // ToDo - Implement here
            throw new NotImplementedException();
          //  var result = await _srvCommonExchange.Exchange(data.AccountFrom, data.AccountTo, data.Currency, data.Amount);

        }

        public void Execute(TaskToDoSendAsset data, Func<bool, Task> invokeResutl)
        {
            Task.Run(async () =>
            {
                var result  = await ExecuteTask(data);
                await invokeResutl(result);
            } );
        }
    }

}
