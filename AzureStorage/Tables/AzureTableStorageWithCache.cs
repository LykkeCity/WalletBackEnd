using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables
{
    /// <summary>
    /// NoSql хранилище, которое хранит данные в кэше
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AzureTableStorageWithCache<T> : INoSQLTableStorage<T> where T : class,  ITableEntity, new()
    {
        private readonly NoSqlTableInMemory<T> _cache;
        private readonly AzureTableStorage<T> _table;
        private void Init()
        {
            // Вычитаем вообще все элементы в кэш
            foreach (var item in _table)
                _cache.Insert(item);
        }

        public AzureTableStorageWithCache(string connstionString, string tableName, ILog log, bool caseSensitive = true)
        {
            _cache = new NoSqlTableInMemory<T>();
            _table = new AzureTableStorage<T>(connstionString, tableName, log, caseSensitive);
            Init();
        }

        public void Insert(T item, params int[] notLogCodes)
        {

            _table.Insert(item, notLogCodes);
            _cache.Insert(item, notLogCodes);
        }

        public async Task InsertAsync(T item, params int[] notLogCodes)
        {
            await _table.InsertAsync(item, notLogCodes);
            _cache.Insert(item, notLogCodes);
        }

        public void InsertOrMerge(T item)
        {
            _table.InsertOrMerge(item);
            _cache.InsertOrMerge(item);
        }

        public T Replace(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = _table.Replace(partitionKey, rowKey, item);
            _cache.Replace(partitionKey, rowKey, item);

            return result;
        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
           var result = await _table.ReplaceAsync(partitionKey, rowKey, item);
            _cache.Replace(partitionKey, rowKey, item);

            return result;
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = await _table.MergeAsync(partitionKey, rowKey, item);
            _cache.Merge(partitionKey, rowKey, item);
            return result;
        }

        public void InsertOrReplaceBatch(IEnumerable<T> entites)
        {
            var myArray = entites as T[] ?? entites.ToArray();
            _table.InsertOrReplaceBatch(myArray);
            _cache.InsertOrReplaceBatch(myArray);
        }

        public async Task InsertOrReplaceBatchAsync(IEnumerable<T> entites)
        {
            var myArray = entites as T[] ?? entites.ToArray();
            await _table.InsertOrReplaceBatchAsync(myArray);
            _cache.InsertOrReplaceBatch(myArray);
        }

        public T Merge(string partitionKey, string rowKey, Func<T, T> item)
        {
            var result = _table.Merge(partitionKey, rowKey, item);
            _cache.Merge(partitionKey, rowKey, item);

            return result;
        }

        public void InsertOrReplace(T item)
        {
            _table.InsertOrReplace(item);
            _cache.InsertOrReplace(item);
        }

        public async Task InsertOrReplaceAsync(T item)
        {
            await _table.InsertOrReplaceAsync(item);
            _cache.InsertOrReplace(item);
        }

        public void Delete(T item)
        {
            _table.Delete(item);
            _cache.Delete(item);
        }

        public async Task DeleteAsync(T item)
        {
            await _table.DeleteAsync(item);
            _cache.Delete(item);
        }

        public T Delete(string partitionKey, string rowKey)
        {
            var result = _table.Delete(partitionKey, rowKey);
            _cache.Delete(partitionKey, rowKey);
            return result;
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var result = await _table.DeleteAsync(partitionKey, rowKey);
            _cache.Delete(partitionKey, rowKey);
            return result;
        }

        public void CreateIfNotExists(T item)
        {
            _table.CreateIfNotExists(item);
            _cache.CreateIfNotExists(item);
        }

        public void DoBatch(TableBatchOperation batch)
        {
            _table.DoBatch(batch);
            _cache.DoBatch(batch);
        }

        T INoSQLTableStorage<T>.this[string partition, string row] => _cache[partition, row];

        IEnumerable<T> INoSQLTableStorage<T>.this[string partition] => _cache[partition];

        public IEnumerable<T> GetData(Func<T, bool> filter = null) => _cache.GetData(filter);

        public Task<T> GetDataAsync(string partition, string row) => _cache.GetDataAsync(partition, row);

        public Task<IList<T>> GetDataAsync(Func<T, bool> filter = null) => _cache.GetDataAsync(filter);

        public Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100,
            Func<T, bool> filter = null)
            => _cache.GetDataAsync(partitionKey, rowKeys, pieceSize, filter);

        public Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
                        => _cache.GetDataAsync(partitionKeys, pieceSize, filter);


        public Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
                        => _cache.GetDataAsync(keys, pieceSize, filter);

        public Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
                   => _cache.GetDataByChunksAsync(chunks);

        public Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
             => _cache.GetDataByChunksAsync(chunks);

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
             => _cache.GetDataByChunksAsync(partitionKey, chunks);

        public Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
             => _cache.FirstOrNullViaScanAsync(partitionKey, dataToSearch);

        public IEnumerable<T> GetData(string partitionKey, Func<T, bool> filter = null)
             => _cache.GetData(partitionKey, filter);

        public Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
             => _cache.GetDataAsync(partition, filter);

        public Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
             => _cache.GetDataRowKeysOnlyAsync(rowKeys);

        public IEnumerable<T> Where(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
             => _table.Where(rangeQuery, filter);

        public Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
             => _table.WhereAsyncc(rangeQuery, filter);

        public Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
             => _table.WhereAsync(rangeQuery, filter);

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> yieldResult)
             => _table.ExecuteAsync(rangeQuery, yieldResult);


        public IEnumerator<T> GetEnumerator()
             => _cache.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
             => _cache.GetEnumerator();
    }
}
