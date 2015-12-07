using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using LykkeWalletServices.Transactions.TaskHandlers;

namespace LykkeWalletServices
{
    public class SrvQueueReader : TimerPeriod
    {
        private readonly ILykkeAccountReader _lykkeAccountReader;
        private readonly IQueueReader _queueReader;
        private readonly IQueueWriter _queueWriter;
        private readonly ILog _log;

        public SrvQueueReader(ILykkeAccountReader lykkeAccountReader, IQueueReader queueReader, IQueueWriter queueWriter, ILog log) 
            : base("SrvQueueReader", 5000, log)
        {
            _lykkeAccountReader = lykkeAccountReader;
            _queueReader = queueReader;
            _queueWriter = queueWriter;
            _log = log;
        }

        protected override async Task Execute()
        {
            var @event = await _queueReader.GetTaskToDo();

            if (@event == null)
                return;

            var transactionDepositWithdraw = @event as TaskToDoDepositWithdraw;
            if (transactionDepositWithdraw != null)
            {
                var service = new SrvDepositWithdrawTaskHandler(_lykkeAccountReader);
                service.Execute(transactionDepositWithdraw, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create(@event.TransactionId, result));
                },
                    _log);
                return;
            }


            var taskToDoSendAsset = @event as TaskToDoSendAsset;
            if (taskToDoSendAsset != null)
            {
                var service = new SrvExchangeTaskHandler();
                service.Execute(taskToDoSendAsset, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create(@event.TransactionId, result));
                });
                return;
            }

            await _log.WriteWarning("SrvQueueReader", "Execute", "", $"Unknown task type: {@event.GetType()}");

        }
    }
}
