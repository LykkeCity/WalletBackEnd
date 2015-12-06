using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;


namespace Common
{

    public class Indexator<TIndex, TValue>
    {
        private readonly Func<TIndex, TValue> _getValue;

        public Indexator(Func<TIndex, TValue> getValue)
        {
            _getValue = getValue;
        }

        public TValue this[TIndex index]
        {
            get { return _getValue(index); }
        }

    }

    //Todo залочить все операции добавления удаления
    public class IndexedList<TKey, TValue> : IEnumerable<TValue>, INotifyCollectionChanged
    {
        internal object LockObject = new object();

        public class Page : IEnumerable<TValue>, IComparable<Page>
        {
            private readonly SortedList<TKey, TValue> _items;
            private readonly int _capacity;

            public Page(int capacity)
            {
                _capacity = capacity;
                _items = new SortedList<TKey, TValue>(capacity);
                Keys = new Indexator<int, TKey>(index=>_items.Keys[index]);
                Values = new Indexator<int, TValue>(index => _items.Values[index]);
            }

            public Indexator<int, TKey> Keys { get; private set; }

            public Indexator<int, TValue> Values { get; private set; }

            public TValue this[TKey key]
            {
                get { return _items[key]; }
            }

            public bool ContainsKey(TKey key)
            {
                return _items.ContainsKey(key);
            }

            public int Add(TKey key, TValue value)
            {
                if (IsFull)
                    throw new Exception("Page is Full");
                _items.Add(key, value);
                return _items.IndexOfKey(key);
            }

            public int GetIndex(TKey key)
            {
                return _items.IndexOfKey(key);
            }

            public int Count
            {
                get { return _items.Count; }
            }

            public TKey MinKey
            {
                get { return _items.Keys[0]; }
            }

            public TKey MaxKey
            {
                get { return _items.Keys[Count - 1]; }
            }

            #region Implementation of IComparable<in IndexedList<TKey,TValue>.Page>

            public int CompareTo(Page other)
            {
                var thisMinKey = (IComparable<TKey>) MinKey;

                return thisMinKey.CompareTo(other.MinKey);

            }

            #endregion

            public Page Split()
            {
                if (Count < 2)
                    throw new Exception("There is not to split. Items count=" + Count);

                var newPage = new Page(_capacity);

                int fromIndex = Count/2;

                for (var i = fromIndex; i < Count; i++)
                    newPage.Add(_items.Keys[i], _items.Values[i]);

                for (var i = Count - 1; i >= fromIndex; i--)
                    _items.RemoveAt(i);

                return newPage;

            }

            public bool IsFull
            {
                get { return _items.Count >= _items.Capacity; }
            }

            public bool IsEmpty
            {
                get { return _items.Count == 0; }
            }

            internal void Remove(TKey key)
            {
                _items.Remove(key);
            }

            internal int IndexOffset { get; set; }

            #region Implementation of IEnumerable

            public IEnumerator<TValue> GetEnumerator()
            {
                return _items.Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion
        }

        private readonly int _pageCapacity;

        private readonly List<Page> _pages = new List<Page>(1024);

        public ReadOnlyCollection<Page> Pages { get; private set; }

        private int WhichPageIndex(int index, IComparable<TKey> keyComparator)
        {

            if (keyComparator.CompareTo(_pages[index + 1].MinKey) >= 0)
                return index + 1;

            return index;

        }

        /// <summary>
        /// Get index of pages which you can find the Key or inster new item
        /// </summary>
        /// <param name="keyComparator"></param>
        /// <returns>index, where Page fits to Add Item
        /// Count, if new page Requesed. It's nessesery add this page to the end of the list of pages</returns>
        public int GetPageIndex(IComparable<TKey> keyComparator)
        {
            if (_pages.Count == 0)
                return 0;

            int lowRange = 0;
            int highRange = _pages.Count - 1;

            //Если элемент надо добавить в конец массива и последняя страница полная
            //Оптимально сделать новую страницу и туда воткнуть элемент
            if (keyComparator.CompareTo(_pages[highRange].MaxKey) > 0)
                if (_pages[highRange].IsFull)
                    return _pages.Count;


            while (highRange - lowRange > 1)
            {
                var midRange = (highRange + lowRange)/2;


                // If key between Min and Max key value at page
                if (keyComparator.CompareTo(_pages[midRange].MinKey) >= 0 &&
                    keyComparator.CompareTo(_pages[midRange].MaxKey) <= 0)
                    return midRange;

                // If key value above then Max Value on the page
                if (keyComparator.CompareTo(_pages[midRange].MaxKey) > 0)
                    lowRange = midRange;
                else
                    // If key value above then Max Value on the page
                    if (keyComparator.CompareTo(_pages[midRange].MinKey) < 0)
                        highRange = midRange;

            }

            if (highRange == lowRange)
                return lowRange;


            return WhichPageIndex(lowRange, keyComparator);

        }

        public int GetPageIndex(TKey key)
        {
            return GetPageIndex((IComparable<TKey>) key);
        }

        public Page GetPageForNewItem(TKey key, out int pageIndex)
        {

            var keyComparator = (IComparable<TKey>) key;

            pageIndex = GetPageIndex(keyComparator);

            if (pageIndex == _pages.Count)
            {
                var lastPage = new Page(_pageCapacity);

                _pages.Add(lastPage);
                return (lastPage);
            }

            var page = _pages[pageIndex];

            if (!page.IsFull)
                return page;

            var newPage = page.Split();

            _pages.Insert(pageIndex + 1, newPage);

            return _pages[WhichPageIndex(pageIndex, keyComparator)];

        }

        public TValue this[TKey key]
        {
            get
            {
                var pageIndex = GetPageIndex(key);

                if (pageIndex == _pages.Count)
                    throw new Exception("Item " + key + " not found.");

                return _pages[pageIndex][key];
            }
        }

        public bool ContainsKey(TKey key)
        {
            var pageIndex = GetPageIndex(key);

            return pageIndex != _pages.Count && _pages[pageIndex].ContainsKey(key);
        }

        #region Implementation of IEnumerable

        public IEnumerator<TValue> GetEnumerator()
        {
            return _pages.SelectMany(page => page).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public IndexedList(int pageCapacity = 256)
        {
            Count = 0;
            _pageCapacity = pageCapacity;
            _pages = new List<Page>(1024);
            Pages = new ReadOnlyCollection<Page>(_pages);

            Keys = new Indexator<int, TKey>(GetKey);
            Values = new Indexator<int, TValue>(GetValue);
        }

        private Page CalcPageByIndex(int index, out int pageIndex)
        {
            pageIndex = index;

            foreach (var page in Pages)
            {
                if (pageIndex >= page.Count)
                    pageIndex -= page.Count;
                else
                    return page;
            }

            throw new Exception("Range check error.");
            
        }

        public Indexator<int, TKey> Keys { get; private set; }

        private TKey GetKey(int index)
        {
            int pageIndex;
            var page = CalcPageByIndex(index, out pageIndex);
            return page.Keys[pageIndex];
        }

        public Indexator<int, TValue> Values { get; private set; }

        private TValue GetValue(int index)
        {
            int pageIndex;
            var page = CalcPageByIndex(index, out pageIndex);
            return page.Values[pageIndex];
        }

        private void RefreshCacheIndexes(int fromIndex)
        {
            for (var i = fromIndex; i<_pages.Count; i++)
            {
                if (i == 0)
                    _pages[i].IndexOffset = 0;
                else
                {
                    var prevPage = _pages[i - 1];
                    _pages[i].IndexOffset = prevPage.IndexOffset + prevPage.Count;
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            int pageIndex;
            var page = GetPageForNewItem(key, out pageIndex);
            RefreshCacheIndexes(pageIndex);
            var itemIndexOnPage = page.Add(key, value);
            RaiseCollectionChanged(NotifyCollectionChangedAction.Add, value, page.IndexOffset + itemIndexOnPage);
        }

        public void Remove(TKey key)
        {
            var pageIndex = GetPageIndex(key);
            var page = _pages[pageIndex];

            RaiseCollectionChanged(NotifyCollectionChangedAction.Remove, page[key], page.IndexOffset+page.GetIndex(key));
            page.Remove(key);

            if (page.IsEmpty)
                _pages.RemoveAt(pageIndex);

            RefreshCacheIndexes(pageIndex);

        }

        public void RemoveAt(int index)
        {
            var key = GetKey(index);
            Remove(key);
        }

        public void Clear()
        {
            _pages.Clear();
            RaiseCollectionChanged(NotifyCollectionChangedAction.Reset);

        }

        #region Implementation of INotifyCollectionChanged

        protected void RaiseCollectionChanged(NotifyCollectionChangedAction action, object item=null, int index=0 )
        {
            lock(this)
            {
                switch (action)
                {
                        case NotifyCollectionChangedAction.Add:
                        Count++;
                        break;

                        case NotifyCollectionChangedAction.Remove:
                        Count--;
                        break;

                        case NotifyCollectionChangedAction.Reset:
                        Count = 0;
                        break;
                }

                if (CollectionChanged != null)
                {  
                    var nccea = new NotifyCollectionChangedEventArgs(action, item, index);
                    CollectionChanged(this, nccea);
                }

            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion


        #region Implementatio of IRemoteGuiTable



        #endregion

        public int Count { get; private set; }


    }

}
