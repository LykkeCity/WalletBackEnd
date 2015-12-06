using System.Threading.Tasks;
using Core;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvExchangeTaskHandler
    {

        public async void ExecuteTask(TaskToDoSendAsset data, IQueueWriter queueWriter)
        {
          //  var result = await _srvCommonExchange.Exchange(data.AccountFrom, data.AccountTo, data.Currency, data.Amount);

        }

        public void Execute(TaskToDoSendAsset data, IQueueWriter queueWriter)
        {
            Task.Run(() => ExecuteTask(data, queueWriter));
        }
    }

}
