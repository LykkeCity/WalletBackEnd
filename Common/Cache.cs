using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Common
{
    public class Cache<TKey, TValue>
    {
        private readonly Func<TKey, TValue> _getValue;
        private readonly Dictionary<TKey, object> _lockObjects = new Dictionary<TKey, object>();

        private readonly Dictionary<TKey, TValue> _items = new Dictionary<TKey, TValue>();


        public Cache(Func<TKey, TValue> getValue)
        {
            _getValue = getValue;
        }

        public TValue this[TKey key]
        {
            get
            {
                object localLockObject;
                lock (_lockObjects)
                {
                    if (_lockObjects.ContainsKey(key))
                        localLockObject = _lockObjects[key];
                    else
                        _lockObjects.Add(key, localLockObject = new object());
                }

                lock (localLockObject)
                {
                    if (_items.ContainsKey(key))
                        return _items[key];

                    var newValue = _getValue(key);
                    _items.Add(key, newValue);

                    return newValue;
                }

            }
        }
    }

    public class CacheAsync<TKey, TValue>
    {
        public class CashItem
        {
            private TValue _value;
            public DateTime LastDateTime { get; set; }

            public TValue Value
            {
                get
                {
                    LastDateTime = DateTime.UtcNow;
                    return _value;
                }
            }

            public static CashItem Create(TValue value)
            {
                return new CashItem
                {
                    _value = value,
                    LastDateTime = DateTime.UtcNow
                };
            }

        }

        private readonly Func<TKey, Task<TValue>> _getValue;
        private readonly int _maxSize;

        private readonly Dictionary<TKey, CashItem> _items = new Dictionary<TKey, CashItem>();


        /// <summary>
        /// Селать кэш из элементов
        /// </summary>
        /// <param name="getValue"></param>
        /// <param name="maxSize"></param>
        public CacheAsync(Func<TKey, Task<TValue>> getValue, int maxSize = 0)
        {
            _getValue = getValue;
            _maxSize = maxSize;
        }

        private void DeleteIfAlot()
        {

            if (_items.Count<=_maxSize)
                return;

            var toDelete = _items.OrderBy(itm => itm.Value.LastDateTime).Select(itm => itm.Key).ToArray();


            foreach (var key in toDelete)
            {
                if (_items.Count <= _maxSize)
                    break;

                _items.Remove(key);
            }


        }

        public async Task<TValue> GetValue(TKey key)
        {

            lock (_items)
            {
                if (_items.ContainsKey(key))
                    return _items[key].Value;
            }

            var newValue = await _getValue(key);

            lock (_items)
            {
                if (_items.ContainsKey(key))
                    return _items[key].Value;

                _items.Add(key, CashItem.Create(newValue));


                if (_maxSize>0)
                  DeleteIfAlot();
    
                return newValue;
            }

        }
    }

}
