using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace AzureStorage
{

    public class AzureQueue<T> : IQueue<T> where T : class
    {
        private readonly CloudQueue _queue;

        public AzureQueue(string conectionString, string queueName)
        {
            queueName = queueName.ToLower();
            var storageAccount = CloudStorageAccount.Parse(conectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();

            _queue = queueClient.GetQueueReference(queueName);
            _queue.CreateIfNotExists();
        }

        public void PutMessage(T itm)
        {
            var msg = Newtonsoft.Json.JsonConvert.SerializeObject(itm);
            _queue.AddMessage(new CloudQueueMessage(msg));
        }

        public Task PutMessageAsync(T itm)
        {
            var msg = Newtonsoft.Json.JsonConvert.SerializeObject(itm);
            return _queue.AddMessageAsync(new CloudQueueMessage(msg));
        }

        public T GetMessage()
        {

            var msg = _queue.GetMessage();

            if (msg == null)
                return null;
            _queue.DeleteMessage(msg);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(msg.AsString);
        }

        public async Task<T> GetMessageAsync()
        {
            var msg = await _queue.GetMessageAsync();
            if (msg == null)
                return null;

            await _queue.DeleteMessageAsync(msg);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(msg.AsString);
        }

        public QueueMessageToken<T> GetMessageAndHide()
        {
            var msg = _queue.GetMessage();

            if (msg == null)
                return null;

            return QueueMessageToken<T>.Create(
                Newtonsoft.Json.JsonConvert.DeserializeObject<T>(msg.AsString),
                msg
                );

        }

        public void ProcessMessage(QueueMessageToken<T> msg)
        {
            var token = msg.Token as CloudQueueMessage;

            if (token == null)
                return;

            _queue.DeleteMessage(token);
        }

        public IEnumerable<T> PeekAllMessages(int maxCount)
        {
            return _queue.PeekMessages(maxCount).Select(msg => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(msg.AsString));
        }

        public async Task<IEnumerable<T>> PeekAllMessagesAsync(int maxCount)
        {
            return (await _queue.PeekMessagesAsync(maxCount)).Select(msg => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(msg.AsString));
        }


        public void Clear()
        {
            _queue.Clear();
        }

        public Task ClearAsync()
        {
            return _queue.ClearAsync();
        }

        public int Size
        {
            get
            {
                var msg = _queue.PeekMessages(20).ToArray();
                return msg.Length;
            }
        }

        public async Task<int> GetSizeAsync()
        {
                var msg = (await _queue.PeekMessagesAsync(20)).ToArray();
                return msg.Length;
        }
    }

}
