using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common
{

    public class DictionaryThreadSafe<TKey, TValue> where TValue : class 
    {
        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();


        public void Add(TKey key, TValue value)
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

        public void AddIfNotExists(TKey key, TValue value)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (!_items.ContainsKey(key))
                  _items.Add(key, value);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }            
        }

        public void Remove(TKey key)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _items.Remove(key);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }            
        }

        public void RemoveIfExists(TKey key)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (_items.ContainsKey(key))
                _items.Remove(key);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }


        public TValue this[TKey key]
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

        public TValue FindOrDefault(TKey key)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _items.ContainsKey(key) ? _items[key] : null;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public void Modify(TKey key, Action<TValue> modifyAction)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (_items.ContainsKey(key))
                    modifyAction(_items[key]);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }            
        }


        public IEnumerable<TValue> Values
        {
            get
            {
                _lockSlim.EnterReadLock();
                try
                {
                    return _items.Values.ToArray();
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }
            }
        }

        public IEnumerable<TResult> Select<TResult>(Func<TValue, TResult> create)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _items.Values.Select(create).ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IEnumerable<TValue> Where(Func<TValue, bool> predicate)
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _items.Values.Where(predicate).ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IEnumerable<TResult> WhereAndSelect<TResult>(Func<TValue, bool> predicate, Func<TValue, TResult> selectAction)
        {
            IEnumerable<TValue> result;

            _lockSlim.EnterReadLock();
            try
            {
                result = _items.Values.Where(predicate).ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            return result.Select(selectAction);
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

    }


    public static class DictionaryThreadSafeExtentions
    {

        public static void AddThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            lock (dictionary)
                dictionary.Add(key, value);
        }


        public static void RemoveThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            lock (dictionary)
                dictionary.Remove(key);
        }


        public static TValue[] WhereThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> source, Func<TValue, bool> predicate)
        {
            lock (source)
                return source.Values.Where(predicate).ToArray();
        }


        public static void ModifyThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Action<TValue> modifyAction)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                    modifyAction(dictionary[key]);
            }  
        }

        public static TValue FindThreadSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            lock (dictionary)
                return dictionary[key];
        }
    }
}
