using System;
using System.Collections.Generic;
using System.Threading;

namespace Common.RemoteUi
{
    public interface IGuiTable
    {
        GuiTableData TableData { get; }
    }


    public class GuiTableData
    {
        public string[] Headers { get; set; }
        public List<string[]> Data { get; set; }
    }


    public interface ITableDataList
    {
        IGuiTable this[string id] { get; }
        bool ContainId(string id);
    }

    // Имплементируем во все классы, в которые попадают данные
    public interface IGuiTableDataSrc
    {
        void NewData(params string[] data);
        void Clear();
    }

    public interface IGuiTableDataProfileSrc : IGuiTableDataSrc
    {
        void DelData(string key);
    }

    /// <summary>
    /// Вспомогательный класс, который решает проблемы с индикацией разрезов данных по ключу
    /// </summary>
    public class GuiTableProfile : IGuiTable, IGuiTableDataProfileSrc
    {
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly SortedDictionary<string, string[]> _items = new SortedDictionary<string, string[]>();
        private readonly int _maxLines;
        private readonly string[] _headers;

        /// <summary>
        /// Нулевой элемент - ключ
        /// </summary>
        /// <param name="maxLines">Максимальное количество записей</param>
        /// <param name="headers">заголовок</param>
        public GuiTableProfile(int maxLines, params string[] headers)
        {
            if (headers == null)
                throw new Exception("Can not create GuiTableProfile. headers is null");

            if (headers.Length == 0)
                throw new Exception("Can not create GuiTableProfile. headers.Length=0");

            _maxLines = maxLines;
            _headers = headers;
        }


        /// <summary>
        /// 0 элемент - это ключ;
        /// </summary>
        /// <param name="data"></param>
        public void NewData(params string[] data)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                if (!_items.ContainsKey(data[0]))
                {
                    if (_items.Count<_maxLines)
                      _items.Add(data[0], data);
                }

                else
                    _items[data[0]] = data;

            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

        }

        public void Clear()
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _items.Clear();
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void DelData(string key)
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

        public GuiTableData TableData
        {
            get
            {
                var result = new GuiTableData {Headers = _headers, Data = new List<string[]>()};

                _lockSlim.EnterReadLock();
                try
                {
                    foreach (var item in _items.Values)
                        result.Data.Add(item);  
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }

                return result;
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
    }


    public class GuiTableLastData : IGuiTable, IGuiTableDataSrc
    {

        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        private readonly List<string[]> _items = new List<string[]>();

        private readonly string[] _headers;

        private readonly int _maxCount;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxCount">Максимальное число записей </param>
        /// <param name="headers"></param>
        public GuiTableLastData(int maxCount, params string[] headers)
        {
            _maxCount = maxCount;

            if (headers == null)
                throw new Exception("Can not create GuiTableProfile. headers is null");

            if (headers.Length == 0)
                throw new Exception("Can not create GuiTableProfile. headers.Length=0");

            _headers = headers;


        }


        public void NewData(params string[] data)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                
              _items.Insert(0, data);

              if (_items.Count > _maxCount)
                  _items.RemoveAt(_items.Count - 1);

            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

        }

        public void Clear()
        {
            _lockSlim.EnterWriteLock();
            try
            {

                _items.Clear();

            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public GuiTableData TableData
        {
            get
            {
                var result = new GuiTableData { Headers = _headers, Data = new List<string[]>() };

                _lockSlim.EnterReadLock();
                try
                {
                    foreach (var item in _items)
                        result.Data.Add(item);
                }
                finally
                {
                    _lockSlim.ExitReadLock();
                }

                return result;
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
    }


    public class GuiTable : IGuiTable
    {
        private readonly string[] _headers;
        private readonly Func<List<string[]>> _getData;


        public GuiTable(string[] headers, Func<List<string[]>> getData)
        {
            _headers = headers;
            _getData = getData;
        }


        public GuiTableData TableData
        {
            get
            {

                return new GuiTableData
                {
                    Headers = _headers,
                    Data = _getData()
                };
            }
        }
    }


    public class GuiTableDataDynamic : IGuiTable
    {
        private readonly Func<List<string[]>> _getData;
        private readonly string[] _headers;

        public GuiTableDataDynamic(Func<List<string[]>> getData, params string[] headers)
        {
            _getData = getData;
            _headers = headers;
        }


        public GuiTableData TableData
        {
            get
            {
                return new GuiTableData
                {
                    Headers = _headers,
                    Data = _getData()
                };
            }
        }
    }

}
