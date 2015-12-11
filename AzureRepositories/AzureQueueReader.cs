﻿using System.Threading.Tasks;
using Common;
using Core;

namespace AzureRepositories
{
    public class AzureQueueReader : IQueueReader
    {
        private readonly IQueueExt _queueExt;

        public AzureQueueReader(IQueueExt queueExt)
        {
            _queueExt = queueExt;

            _queueExt.RegisterTypes(
                QueueType.Create("GetBalance", typeof(TaskToDoGetBalance)),
                QueueType.Create("DepositWithdraw", typeof (TaskToDoDepositWithdraw)), 
                QueueType.Create("Exchange", typeof (TaskToDoSendAsset)));
        }

        public async Task<TransactionToDoBase> GetTaskToDo()
        {
            // ToDo - improve - make sure that we shold Delete it only after we put it to our Local Storage. For first stage is good
            var message = await _queueExt.GetMessageAsync();
            return message as TransactionToDoBase;
        }

    }
}
