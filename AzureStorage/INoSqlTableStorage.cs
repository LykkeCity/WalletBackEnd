using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage
{

    public interface INoSQLTableStorage<T> : IEnumerable<T> where T : ITableEntity, new()
    {

        /// <summary>
        /// Добавить новый элемент в таблице
        /// </summary>
        /// <param name="item">Элемент, который нужно вставить</param>
        /// <param name="notLogCodes">не логировать эксепшены с кодами согласно списку</param>
        void Insert(T item, params int[] notLogCodes);

        // Добавить новый элемент асинхронно (элемент вообще не должен существовать)
        Task InsertAsync(T item, params int[] notLogCodes);


        // Добавить новый элемент, или добавиить поля к существующему элементу
        void InsertOrMerge(T item);

        // Полностью заменть существующий элемент на новый
        T Replace(string partitionKey, string rowKey, Func<T, T> item);
        Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item);

        Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item);


        void InsertOrReplaceBatch(IEnumerable<T> entites);
        Task InsertOrReplaceBatchAsync(IEnumerable<T> entites);


        // Добавить к существующему элементу новые поля
        T Merge(string partitionKey, string rowKey, Func<T, T> item);



        /// <summary>
        /// Добавляет новый элемент, или заменяет полностью существующий
        /// </summary>
        /// <param name="item"></param>
        void InsertOrReplace(T item);

        Task InsertOrReplaceAsync(T item);

        /// <summary>
        /// Удаляет элемент из таблицы
        /// </summary>
        /// <param name="item"></param>
        void Delete(T item);

        Task DeleteAsync(T item);

        T Delete(string partitionKey, string rowKey);

        Task<T> DeleteAsync(string partitionKey, string rowKey);

        void CreateIfNotExists(T item);


        /// <summary>
        /// Выполнить набор операций
        /// </summary>
        /// <param name="batch"></param>
        void DoBatch(TableBatchOperation batch);

        /// <summary>
        /// Возвращает элемент
        /// </summary>
        /// <param name="partition">Партиция</param>
        /// <param name="row">Колонка</param>
        /// <returns>null или объект</returns>
        T this[string partition, string row] { get; }

        IEnumerable<T> this[string partition] { get; }
        IEnumerable<T> GetData(Func<T, bool> filter = null);


        Task<T> GetDataAsync(string partition, string row);


        /// <summary>
        /// Получить записи, предварительно отфильтровав их
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<IList<T>> GetDataAsync(Func<T, bool> filter = null);

        /// <summary>
        /// Запрос по одной партиции и нескольким элементам
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKeys"></param>
        /// <param name="pieceSize">На сколько частейделим запрос</param>
        /// <param name="filter">Фильтрация записей</param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null);

        Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null);

        Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null);


        Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks);

        Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks);
        Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks);

        /// <summary>
        /// Scan table by chinks and find an instane
        /// </summary>
        /// <param name="partitionKey">Partition we are going to scan</param>
        /// <param name="dataToSearch">CallBack, which we going to call when we have chunk of data to scan. </param>
        /// <returns>Null or instance</returns>
        Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch);

        IEnumerable<T> GetData(string partitionKey, Func<T, bool> filter = null);
        Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null);

        Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys);

        IEnumerable<T> Where(TableQuery<T> rangeQuery, Func<T, bool> filter = null);
        Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null);
        Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null);

        /// <summary>
        /// Асинхронное получение данных. Можно использовать для экономии памяти запроса.
        /// </summary>
        /// <param name="rangeQuery">Запрос</param>
        /// <param name="yieldResult">обратные вызовы кусками с информацией, полученной по сети</param>
        /// <returns>Task</returns>
        Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult);

    }



}
