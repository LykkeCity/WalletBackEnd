using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Common
{
    public interface ILock
    {
        object LockObject { get; }
    }

    public class DispatchedCollection<T> : INotifyCollectionChanged, IDisposable, IEnumerable<T>
    {
        private readonly INotifyCollectionChanged _ncc;
        private readonly IEnumerable<T> _enumerable;
        private readonly ILock _lockObject;

        private readonly Action<object, NotifyCollectionChangedEventArgs> _invoke;

        public DispatchedCollection(object source, Action<object, NotifyCollectionChangedEventArgs> invoke)
        {
            _ncc = source as INotifyCollectionChanged;

            if (_ncc == null)
                throw new Exception("source of DispatchedCollection has to implement INotifyCollectionChanged interface");

            _enumerable = source as IEnumerable<T>;

            if (_ncc == null)
                throw new Exception("source of DispatchedCollection has to implement IEnumerable interface");

            _lockObject = source as ILock;

            if (_ncc == null)
                throw new Exception("source of DispatchedCollection has to implement ILock interface");


            _invoke = invoke;

            _ncc.CollectionChanged += CollectionChangedEvent;
        }

        void CollectionChangedEvent(object sender, NotifyCollectionChangedEventArgs e)
        {
            _invoke(sender, e);
        }

        #region Implementation of INotifyCollectionChanged

        public void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs eventArgs)
        {
            lock (_lockObject.LockObject)
            {
                if (_collectionChanged != null)
                    _collectionChanged(sender, eventArgs);
            }
        }

        private NotifyCollectionChangedEventHandler _collectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                lock (_lockObject.LockObject)
                    _collectionChanged += value;
            }
            remove
            {
                lock (_lockObject.LockObject)
                    _collectionChanged -= value;
            }
            
        }

        #endregion

        public void Dispose()
        {
            _ncc.CollectionChanged -= CollectionChangedEvent;
        }

        #region Implementation of IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            lock (_lockObject.LockObject)
              return _enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lockObject.LockObject)
              return _enumerable.GetEnumerator();
        }

        #endregion
    }
}
