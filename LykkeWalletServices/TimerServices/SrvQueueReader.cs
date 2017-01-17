using Common;
using Common.Log;
using Core;
using LykkeWalletServices.Transactions.TaskHandlers;
using NBitcoin;
using System;
using System.Threading.Tasks;
using Common.IocContainer;
using Core.LykkeIntegration.Services;

namespace LykkeWalletServices
{
    public class SrvQueueReader : TimerPeriod
    {
        public ITransactionProcessor TransactionProcessor
        {
            get;
            set;
        }

        private readonly IQueueReader _queueReader;
        
        public SrvQueueReader(IQueueReader queueReader, ILog log,
            int queueReaderIntervalInMiliseconds)
            : base("SrvQueueReader", queueReaderIntervalInMiliseconds, log)
        {
            _queueReader = queueReader;
        
        }

        protected override async Task Execute()
        {
            var @event = await _queueReader.GetTaskToDo();

            if (@event == null)
                return;

            await TransactionProcessor.Process(@event);
        }
    }
}
