using System;
using System.Collections.Generic;

namespace Common
{
    public class CashRepository<TKey, TCompositeKey, TValue>
    {
        private readonly Func<TCompositeKey, TKey> _getKey;
        private readonly Func<TKey, TCompositeKey, TValue> _getValue;

        private readonly Dictionary<TKey, TValue> _values = new Dictionary<TKey, TValue>(); 

        public CashRepository(Func<TCompositeKey, TKey> getKey, Func<TKey, TCompositeKey, TValue> getValue)
        {
            _getKey = getKey;
            _getValue = getValue;
        }


        public TValue this[TCompositeKey compKey]
        {
            get
            {
                lock (_values)
                {
                    var key = _getKey(compKey);

                    if (_values.ContainsKey(key))
                        return _values[key];

                    var value = _getValue(key, compKey);
                    _values.Add(key, value);
                    return value;
                }
            }

        }

    }

}
