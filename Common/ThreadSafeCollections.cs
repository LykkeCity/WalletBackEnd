using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Common.TypeMappers;

namespace Common
{
    public class DictionaryThreadSafe<TKey, TValue, TInterface> : IListReadOnly<TInterface> where TValue : TInterface
    {
        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        protected void Add(TKey key, TValue value)
        {
            _lockSlim.EnterWriteLock();

            try
            {
               _items.Add(key, value);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }   
        }

        public TInterface this[TKey key]
        {
            get
            {
                _lockSlim.EnterReadLock();

                try
                {
                    return _items[key];
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }  
            }
        }
        
        public int Count
        {
            get
            {
                _lockSlim.EnterReadLock();

                try
                {
                    return _items.Count;
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                } 
            }
        }

        public IEnumerator<TInterface> GetEnumerator()
        {
            _lockSlim.EnterReadLock();

            try
            {
                foreach (var value in _items.Values)
                {
                    yield return value;
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

}
