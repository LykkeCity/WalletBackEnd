using System.Collections.Generic;
using System.Threading;

namespace Common.ThreadSafe
{
    public class QueueThreadSafe<T>
    {
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly Queue<T> _queue = new Queue<T>(); 

        public void Put(T item)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _queue.Enqueue(item);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public T Get()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _queue.Dequeue();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _queue.Count;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
                
            }
        }
    }
}
