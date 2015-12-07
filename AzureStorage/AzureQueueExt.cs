using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage
{
    public class AzureQueueExt : IQueueExt
    {
        private readonly CloudQueue _queue;

        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>(); 

        public AzureQueueExt(string conectionString, string queueName, params QueueType[] types)
        {
            queueName = queueName.ToLower();
            var storageAccount = CloudStorageAccount.Parse(conectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();

            _queue = queueClient.GetQueueReference(queueName);

            _queue.CreateIfNotExists();

            RegisterTypes(types);
        }

        private static string SerializeObject(object itm)
        {
            return itm.GetType()+":"+Newtonsoft.Json.JsonConvert.SerializeObject(itm);
        }

        private object DeserializeObject(string itm)
        {
            var i = itm.IndexOf(':');

            var typeStr = itm.Substring(0, i);

            if (!_types.ContainsKey(typeStr))
                return null;

            var data = itm.Substring(i + 1, itm.Count() - i-1);

            return Newtonsoft.Json.JsonConvert.DeserializeObject(data, _types[typeStr]);
            
        }

        public void PutMessage(object itm)
        {
            var msg = SerializeObject(itm);
            _queue.AddMessage(new CloudQueueMessage(msg));
        }

        public Task PutMessageAsync(object itm)
        {
            var msg = SerializeObject(itm);
            return _queue.AddMessageAsync(new CloudQueueMessage(msg));
        }

        public object GetMessage()
        {
            var msg = _queue.GetMessage();

            if (msg == null)
                return null;

            _queue.DeleteMessage(msg);
            return DeserializeObject(msg.AsString);
        }

        public async Task<object> GetMessageAsync()
        {
            var msg = await _queue.GetMessageAsync();

            if (msg == null)
                return null;

            _queue.DeleteMessage(msg);
            return DeserializeObject(msg.AsString);
        }

        public object[] GetMessages(int maxCount)
        {
            var messages = _queue.GetMessages(maxCount);

            var cloudQueueMessages = messages as CloudQueueMessage[] ?? messages.ToArray();
            foreach (var cloudQueueMessage in cloudQueueMessages)
                _queue.DeleteMessage(cloudQueueMessage);
          
            return cloudQueueMessages
                .Select(message => DeserializeObject(message.AsString))
                .Where(itm => itm != null).ToArray();
        }

        public async Task<object[]> GetMessagesAsync(int maxCount)
        {
            var messages = await _queue.GetMessagesAsync(maxCount);

            var cloudQueueMessages = messages as CloudQueueMessage[] ?? messages.ToArray();
            foreach (var cloudQueueMessage in cloudQueueMessages)
                await _queue.DeleteMessageAsync(cloudQueueMessage);

            return cloudQueueMessages
                .Select(message => DeserializeObject(message.AsString))
                .Where(itm => itm != null).ToArray();
        }

        public void Clear()
        {
            _queue.Clear();
        }

        public void RegisterTypes(params QueueType[] types)
        {
            foreach (var type in types)
                _types.Add(type.Id, type.Type);
        }
    }
}
