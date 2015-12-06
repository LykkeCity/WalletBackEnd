using System;
using System.Threading.Tasks;
using Common.Log;

namespace Common
{
    public abstract class ProducerConsumer<T> where T:class 
    {
        private readonly string _componentName;
        private readonly ILog _log;

        private readonly AsyncQueue<T> _queue = new AsyncQueue<T>();

        protected abstract Task Consume(T item);

        protected ProducerConsumer(string componentName, ILog log)
        {
            _componentName = componentName;
            _log = log;
        }

        private async Task Handler()
        {
            while (_started)
            {
                try
                {
                    var item = await _queue.DequeueAsync();
                    await Consume(item);
                }
                catch (Exception exception)
                {
                    await _log.WriteError(_componentName, "Handle", "", exception);
                }
            }
        }

        protected void Produce(T item)
        {
            lock (_queue)
                _queue.Enqueue(item); 
  
            Start();
        }



        private readonly object _lockobject = new object();
        private bool _started;
        protected void Start()
        {
            lock (_lockobject)
            {
                if (_started)
                    return;
                _started = true;

                Task.Run(async () => await Handler());
            }
        }



    }



    public class ProducerConsumerLambda<T> : ProducerConsumer<T> where T : class
    {
        private readonly Func<T, Task> _consumeFunc;

        public ProducerConsumerLambda(string componentName, ILog log, Func<T, Task> consumeFunc) : base(componentName, log)
        {
            _consumeFunc = consumeFunc;
        }

        protected override Task Consume(T item)
        {
            return _consumeFunc(item);
        }

        public new void Produce(T item)
        {
            base.Produce(item);
        } 
    }

}
