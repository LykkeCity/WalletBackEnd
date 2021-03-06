﻿using System.Threading.Tasks;
using Common;
using Core;

namespace AzureRepositories
{
    public class AzureQueueWriter : IQueueWriter
    {
        private readonly IQueueExt _queueWriter;

        public AzureQueueWriter(IQueueExt queueWriter)
        {
            _queueWriter = queueWriter;
        }

        public Task WriteQueue(TransactionResultModel transactionResult)
        {
            // ToDo - Handle connection absents. For instance: put to local queue and after try to send to the realone
            string message = transactionResult.OperationName + ":" +
                Newtonsoft.Json.JsonConvert.SerializeObject(transactionResult);

            //return _queueWriter.PutMessageAsync(transactionResult);
            return _queueWriter.PutMessageAsync(message);
        }
    }
}
