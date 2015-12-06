using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;


namespace Common
{
    public class SortedDictionaryObservable<TKey, TValue> : INotifyCollectionChanged, IDictionary<TKey, TValue>
    {

        private readonly SortedDictionary<TKey, TValue> _items = new SortedDictionary<TKey, TValue>();

        public bool ContainsKey(TKey key)
        {
            return _items.ContainsKey(key);
        }

        public int IndexOf(TKey key)
        {
            var i = 0;

            foreach (var itm in _items)
            {
                if (itm.Key.Equals(key))
                    return i; 

                i++;

            }

            throw new Exception(key+" is not exists");

        }

        public void Add(TKey key, TValue value)
        {
            _items.Add(key, value);

            NotifyAdded(value, IndexOf(key));

        }

        public bool Remove(TKey key)
        {
            var itm = _items[key];

            NotifyRemoved(itm, IndexOf(key));

            return _items.Remove(key);

        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _items.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get { return _items[key]; }
            set { _items[key] = value; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the keys of the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<TKey> Keys
        {
            get { return _items.Keys; }
        }

        /// <summary>
        /// Gets an <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.Generic.ICollection`1"/> containing the values in the object that implements <see cref="T:System.Collections.Generic.IDictionary`2"/>.
        /// </returns>
        public ICollection<TValue> Values
        {
            get { return _items.Values; }
        }

        #region Implementation of INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void NotifyAdded(object itm, int index)
        {
            var args =
               new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itm, index);

            OnNotifyColectionChanged(args);
        }


        private void NotifyRemoved(object itm, int index)
        {
            var args =
               new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, itm, index);

            OnNotifyColectionChanged(args);
        }

        private void NotifyClear()
        {
            var args =
               new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);

            OnNotifyColectionChanged(args);

        }

        private void OnNotifyColectionChanged(NotifyCollectionChangedEventArgs eventArgs)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, eventArgs);
            }
        }

        #endregion

 
        #region Implementation of ICollection<KeyValuePair<TKey,TValue>>

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _items.Add(item.Key, item.Value);
        }

        public void Clear()
        {
           _items.Clear();
            NotifyClear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _items.ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            var result = ContainsKey(item.Key);

            if (result)
                Remove(item.Key);

            return result;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region Implementation of IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
