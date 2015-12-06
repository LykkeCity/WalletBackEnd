using System;
using System.Collections.Generic;
using System.Threading;

namespace Common
{
    public class PoolItem : IDisposable
    {

        private Action<object> _actionDispose;
        protected object Instance { get; private set; }
        public void Init(Action<object> dispose, object instance)
        {
            _actionDispose = dispose;
            Instance = instance;
        }

        public void Dispose()
        {
            _actionDispose(Instance);
        }
    }



    public abstract class Pool<TPoolItem> where TPoolItem : PoolItem, new()
    {

        private readonly int _size;


        private readonly Queue<object> _freeItems = new Queue<object>();
        private readonly object _lockObject = new object();

        protected Pool(int size)
        {
            _size = size;

            _semaphore = new Semaphore(_size, _size);
        }

        private readonly Semaphore _semaphore;

        private void ConnecitonDispose(object item)
        {
            lock (_lockObject)
            {
                OnDisposeItem(item);
                _freeItems.Enqueue(item);
            }
            _semaphore.Release();

        }

        protected abstract object CreateNewItem();


        protected abstract void OnDisposeItem(object item);

        protected TPoolItem GetItem()
        {
            _semaphore.WaitOne();
            lock (_lockObject)
            {
                var newPoolItem = new TPoolItem();
                newPoolItem.Init(ConnecitonDispose, _freeItems.Count > 0 ? _freeItems.Dequeue() : CreateNewItem());
                return newPoolItem;
            }
        }

    }
}
