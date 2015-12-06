using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureStorage.Tables
{

    public class AzureTableStorage<T>: INoSQLTableStorage<T> where T : class, ITableEntity, new()
    {
        private readonly string _connstionString;
        private readonly string _tableName;
        private readonly ILog _log;
        private bool CaseSensitive { get; }

        private CloudStorageAccount _cloudStorageAccount;


        public void DoBatch(TableBatchOperation batch)
        {
            GetTable().ExecuteBatch(batch);
        }

        private bool _tableChecked;

        private CloudTable _table;

        private void CreateTableIfNoExists()
        {
            _cloudStorageAccount = CloudStorageAccount.Parse(_connstionString);
            var cloudTableClient = _cloudStorageAccount.CreateCloudTableClient();
            _table = cloudTableClient.GetTableReference(_tableName);
            _table.CreateIfNotExists();
        }

        private CloudTable GetTable()
        {
            if (!_tableChecked)
            {
                CreateTableIfNoExists();
                _tableChecked = true;
            }

            return _table;
        }


        public AzureTableStorage(string connstionString, string tableName, ILog log, bool caseSensitive = true)
        {
            _connstionString = connstionString;
            _tableName = tableName;
            _log = log;
            CaseSensitive = caseSensitive;
        }

        protected IEnumerable<T> GetItemsByPartition(string partitionKey, Func<T, bool> filter = null)
        {
            var query = CompileTableQuery(partitionKey);
            return ExecuteQuery("GetItemsByPartition", query, filter);
        }

        private IEnumerable<T> ExecuteQuery(string processName, TableQuery<T> rangeQuery, Func<T, bool> filter)
        {
            TableContinuationToken tableContinuationToken = null;
            do
            {
                TableQuerySegment<T> queryResponse;
                try
                {
                    queryResponse = GetTable().ExecuteQuerySegmented(rangeQuery, tableContinuationToken);
                    tableContinuationToken = queryResponse.ContinuationToken;
                }
                catch (Exception ex)
                {
                    _log?.WriteFatalError("Table storage: " + _tableName, processName, rangeQuery.FilterString ?? "[null]", ex);
                    throw;
                }

                foreach (var itm in AzureStorageUtils.ApplyFilter(queryResponse.Results, filter))
                    yield return itm;


            } while (tableContinuationToken != null);

        }



        private async Task ExecuteQueryAsync(string processName, TableQuery<T> rangeQuery, Func<T, bool> filter, Func<IEnumerable<T>, Task> yieldData)
        {

            try
            {
                TableContinuationToken tableContinuationToken = null;
                var table = GetTable();
                do
                {
                    var queryResponse = await table.ExecuteQuerySegmentedAsync(rangeQuery, tableContinuationToken);
                    tableContinuationToken = queryResponse.ContinuationToken;
                    await yieldData(AzureStorageUtils.ApplyFilter(queryResponse.Results, filter));
                }
                while (tableContinuationToken != null);

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, processName, rangeQuery.FilterString ?? "[null]", ex).Wait();
                throw;
            }

        }


        /// <summary>
        /// Выполнить запрос асинхроно
        /// </summary>
        /// <param name="processName">Имя процесса (для лога)</param>
        /// <param name="rangeQuery">Параметры запроса</param>
        /// <param name="filter">Фильтрация запроса</param>
        /// <param name="yieldData">Данные которые мы выдаем наружу. Если возвращается false - данные можно больше не запрашивать</param>
        /// <returns></returns>
        private async Task ExecuteQueryAsync(string processName, TableQuery<T> rangeQuery, Func<T, bool> filter, Func<IEnumerable<T>, bool> yieldData)
        {

            try
            {
                TableContinuationToken tableContinuationToken = null;
                var table = GetTable();
                do
                {
                    var queryResponse = await table.ExecuteQuerySegmentedAsync(rangeQuery, tableContinuationToken);
                    tableContinuationToken = queryResponse.ContinuationToken;
                   var shouldWeContinue = yieldData(AzureStorageUtils.ApplyFilter(queryResponse.Results, filter));
                    if (!shouldWeContinue)
                        break;
                }
                while (tableContinuationToken != null);

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, processName, rangeQuery.FilterString ?? "[null]", ex).Wait();
                throw;
            }

        }

        private async Task ExecuteQueryAsync2(string processName, TableQuery<T> rangeQuery, Func<T, Task<bool>> filter, Func<T, bool> yieldData)
        {

            try
            {
                TableContinuationToken tableContinuationToken = null;
                var table = GetTable();
                do
                {
                    var queryResponse = await table.ExecuteQuerySegmentedAsync(rangeQuery, tableContinuationToken);
                    tableContinuationToken = queryResponse.ContinuationToken;

                    foreach (var itm in queryResponse.Results)
                    {
                        if (filter == null || await filter(itm))
                        {
                            var shouldWeContinue = yieldData(itm);
                            if (!shouldWeContinue)
                                return ;
                        }
                    }

                }
                while (tableContinuationToken != null);

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, processName, rangeQuery.FilterString ?? "[null]", ex).Wait();
                throw;
            }

        }



        public virtual IEnumerator<T> GetEnumerator()
        {
            return GetData().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }



        private void HandleException(T item, Exception ex, IEnumerable<int> notLogCodes)
        {
            var storageException = ex as StorageException;
            if (storageException != null)
            {
                if (!storageException.HandleStorageException(notLogCodes))
                {
                     // Если этот эксепшн не обработан, то логируем его
                    _log?.WriteFatalError("Table storage: " + _tableName, "Insert item",
                        AzureStorageUtils.PrintItem(item), ex);
                }
            }
            else
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Insert item", AzureStorageUtils.PrintItem(item), ex);
            }
        }

        public virtual void Insert(T item, params int[] notLogCodes)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }

                GetTable().Execute(TableOperation.Insert(item));
            }
            catch (Exception ex)
            {
                HandleException(item, ex, notLogCodes);
                throw;
            }
        }

        public virtual async Task InsertAsync(T item, params int[] notLogCodes)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }

                await GetTable().ExecuteAsync(TableOperation.Insert(item));
            }
            catch (Exception ex)
            {
                HandleException(item, ex, notLogCodes);
                throw;
            }
        }

        public virtual void InsertOrMerge(T item)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }


                GetTable().Execute(TableOperation.InsertOrMerge(item));
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "InsertOrMerge item", AzureStorageUtils.PrintItem(item),
                    ex);
            }
        }

        public void TestReplace(T item)
        {
            GetTable().Execute(TableOperation.Replace(item));
        }

        public virtual T Replace(string partitionKey, string rowKey, Func<T, T> replaceAction)
        {
            object itm = "Not read";
            try
            {
                if (!CaseSensitive)
                {
                    partitionKey = partitionKey.ToLower();
                    rowKey = rowKey.ToLower();
                }
                while (true)
                {
                    try
                    {
                        var entity = this[partitionKey, rowKey];
                        if (entity != null)
                        {
                            itm = entity;
                            var result = replaceAction(entity);
                            if (result != null)
                                GetTable().Execute(TableOperation.Replace(result));
                            return result;
                        }
                        else
                        return null;
                    }
                    catch (StorageException e)
                    {
                        // Если поймали precondition fall = 412, значит в другом потоке данную сущность успели поменять
                        // - нужно повторить операцию, пока не исполнится без ошибок
                        if (e.RequestInformation.HttpStatusCode != 412)
                            throw;
                    }
                }

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Replace item", AzureStorageUtils.PrintItem(itm), ex);
                throw;
            }


        }

        public async Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> replaceAction)
        {
            object itm = "Not read";
            try
            {
                if (!CaseSensitive)
                {
                    partitionKey = partitionKey.ToLower();
                    rowKey = rowKey.ToLower();
                }

             
                while (true)
                {
                    try
                    {
                        var entity = await GetDataAsync(partitionKey, rowKey);
                        if (entity != null)
                        {
                            var result = replaceAction(entity);
                            itm = result;
                            if (result != null)
                                await GetTable().ExecuteAsync(TableOperation.Replace(result));

                            return result;
                        }

                        return null;

                    }
                    catch (StorageException e)
                    {
                        // Если поймали precondition fall = 412, значит в другом потоке данную сущность успели поменять
                        // - нужно повторить операцию, пока не исполнится без ошибок
                        if (e.RequestInformation.HttpStatusCode != 412)
                            throw;
                    }
                }

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Replace item", AzureStorageUtils.PrintItem(itm), ex).Wait();
                throw;
            }
        }

        public async Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> mergeAction)
        {
            object itm = "Not read";

            try
            {
                if (!CaseSensitive)
                {
                    partitionKey = partitionKey.ToLower();
                    rowKey = rowKey.ToLower();
                }

                while (true)
                {
                    try
                    {
                        var entity = await GetDataAsync(partitionKey, rowKey);
                        if (entity != null)
                        {
                            var result = mergeAction(entity);
                            itm = result;
                            if (result != null)
                                await GetTable().ExecuteAsync(TableOperation.Merge(result));

                            return result;
                        }
                        return null;
                    }
                    catch (StorageException e)
                    {
                        // Если поймали precondition fall = 412, значит в другом потоке данную сущность успели поменять
                        // - нужно повторить операцию, пока не исполнится без ошибок
                        if (e.RequestInformation.HttpStatusCode != 412)
                            throw;
                    }
                }

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Replace item", AzureStorageUtils.PrintItem(itm), ex).Wait();
                throw;
            }
        }

        public void InsertOrReplaceBatch(IEnumerable<T> entites)
        {
            var operationsBatch = new TableBatchOperation();
            
            foreach (var entity in entites)
                operationsBatch.Add(TableOperation.InsertOrReplace(entity));

            GetTable().ExecuteBatch(operationsBatch);
        }

        public Task InsertOrReplaceBatchAsync(IEnumerable<T> entites)
        {
            var operationsBatch = new TableBatchOperation();

            foreach (var entity in entites)
                operationsBatch.Add(TableOperation.InsertOrReplace(entity));

            return GetTable().ExecuteBatchAsync(operationsBatch);
        }

        public virtual T Merge(string partitionKey, string rowKey, Func<T,T> replaceAction)
        {
            object itm = "Not read";

            try
            {

                if (!CaseSensitive)
                {
                    partitionKey = partitionKey.ToLower();
                    rowKey = rowKey.ToLower();
                }

                while (true)
                {
                    try
                    {
                        var entity = this[partitionKey, rowKey];
                        if (entity != null)
                        {
                            itm = entity;
                            var result = replaceAction(entity);
                            if (result != null)
                                GetTable().Execute(TableOperation.Merge(result));

                        }


                    }
                    catch (StorageException e)
                    {
                        // Если поймали precondition fall = 412, значит в другом потоке данную сущность успели поменять
                        // - нужно повторить операцию, пока не исполнится без ошибок
                        if (e.RequestInformation.HttpStatusCode != 412)
                            throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Merge item", AzureStorageUtils.PrintItem(itm), ex);
                throw;
            }
        }

        /*
        public void Merge(string partitionKey, string rowKey, IDictionary<string, object> fields)
        {

            var tableContext = GetTableContext();

            fields.Add("PartitionKey", partitionKey);
            fields.Add("RowKey", rowKey);


            GetTable().Execute(TableOperation.Merge(fields));
        }
       */ 
        public virtual void InsertOrReplace(T item)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }


                GetTable().Execute(TableOperation.InsertOrReplace(item));
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "InsertOrReplace item", AzureStorageUtils.PrintItem(item),
                    ex);
                throw;
            }

        }

        public virtual async Task InsertOrReplaceAsync(T item)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }


               await GetTable().ExecuteAsync(TableOperation.InsertOrReplace(item));
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "InsertOrReplace item", AzureStorageUtils.PrintItem(item),
                    ex).Wait();
                throw;
            }
        }

        private void DeleteItem(T item)
        {
            try
            {
                GetTable().Execute(TableOperation.Delete(item));
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Delete item", AzureStorageUtils.PrintItem(item), ex);
                throw;
            }

        }

        public virtual void Delete(T item)
        {
            DeleteItem(item);
        }

        public virtual async Task DeleteAsync(T item)
        {
            try
            {
                await GetTable().ExecuteAsync(TableOperation.Delete(item));
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Delete item", AzureStorageUtils.PrintItem(item), ex).Wait();
                throw;
            }

        }

        public virtual T Delete(string partitionKey, string rowKey)
        {
            var itm = GetData(partitionKey, rowKey);
            if (itm != null) DeleteItem(itm);
            return itm;
        }

        public async Task<T> DeleteAsync(string partitionKey, string rowKey)
        {
            var itm = await GetDataAsync(partitionKey, rowKey);
            if (itm != null)
              await DeleteAsync(itm);
            return itm;
        }

        public virtual void CreateIfNotExists(T item)
        {
            try
            {
                if (!CaseSensitive)
                {
                    item.PartitionKey = item.PartitionKey.ToLower();
                    item.RowKey = item.RowKey.ToLower();
                }

                if (this[item.PartitionKey, item.RowKey] == null)
                    InsertOrReplace(item);

            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Create if not exists", AzureStorageUtils.PrintItem(item), ex);

                throw;
            }
        }

        private T GetData(string partition, string row)
        {
            try
            {
                if (!CaseSensitive)
                {
                    partition = partition.ToLower();
                    row = row.ToLower();
                }
                var retrieveOperation = TableOperation.Retrieve<T>(partition, row);
                var retrievedResult = GetTable().Execute(retrieveOperation);
                return (T)retrievedResult.Result;
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Get item by partId and rowId",
                    "partitionId=" + partition + "; rowId=" + row, ex);
                throw;
            }
        }

        public virtual T this[string partition, string row] => GetData(partition, row);

        private TableQuery<T> CompileTableQuery(string partition)
        {
            if (!CaseSensitive)
                partition = partition.ToLower();

            var filter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partition);
            return  new TableQuery<T>().Where(filter);
        }

        public async Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys,
            int pieceSize = 15, Func<T, bool> filter = null)
        {

            var result = new List<T>();

            await Task.WhenAll(
                rowKeys.ToPieces(pieceSize).Select(piece =>
                    ExecuteQueryAsync("GetDataWithMutipleRows",
                        AzureStorageUtils.QueryGenerator<T>.MultipleRowKeys(partitionKey, piece.ToArray()), filter,
                        items =>
                        {
                            lock (result)
                                result.AddRange(items);
                            return true;
                        })
                    )
                );

            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null)
        {

            var result = new List<T>();

            await Task.WhenAll(
                partitionKeys.ToPieces(pieceSize).Select(piece =>
                    ExecuteQueryAsync("GetDataWithMutiplePartitionKeys",
                        AzureStorageUtils.QueryGenerator<T>.MultiplePartitionKeys(piece.ToArray()), filter,
                        items =>
                        {
                            lock (result)
                                result.AddRange(items);
                            return true;
                        })
                    )
                );

            return result;
        }

        public async Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null)
        {
            var result = new List<T>();

            await Task.WhenAll(
                keys.ToPieces(pieceSize).Select(piece =>
                    ExecuteQueryAsync("GetDataWithMoltipleKeysAsync",
                        AzureStorageUtils.QueryGenerator<T>.MultipleKeys(piece), filter,
                        items =>
                        {
                            lock (result)
                                result.AddRange(items);
                            return true;
                        })
                    )
                );

            return result;
        }

        public Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks)
        {
            var rangeQuery = new TableQuery<T>();
            return ExecuteQueryAsync("GetDataByChunksAsync", rangeQuery, null, async itms =>
            {
                await chunks(itms);
            });

        }

        public Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks)
        {
            var rangeQuery = new TableQuery<T>();
            return ExecuteQueryAsync("GetDataByChunksAsync", rangeQuery, null, itms =>
            {
                chunks(itms);
                return true;
            });
        }

        public Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks)
        {
            var query = CompileTableQuery(partitionKey);
            return ExecuteAsync(query, chunks);
        }

        public virtual async Task<T> GetDataAsync(string partition, string row)
        {

            try
            {
                if (!CaseSensitive)
                {
                    partition = partition.ToLower();
                    row = row.ToLower();
                }
                var retrieveOperation = TableOperation.Retrieve<T>(partition, row);
                var retrievedResult = await GetTable().ExecuteAsync(retrieveOperation);
                return (T)retrievedResult.Result;
            }
            catch (Exception ex)
            {
                _log?.WriteFatalError("Table storage: " + _tableName, "Get item async by partId and rowId",
                    "partitionId=" + partition + "; rowId=" + row, ex).Wait();
                throw;
            }
        }


        public async Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch)
        {
            var query = CompileTableQuery(partitionKey);

            T result = null;

            await ExecuteQueryAsync("ScanDataAsync", query, itm => true, 
                itms =>
            {
                result = dataToSearch(itms);
                return result == null;
            });

            return result;

        }

        public virtual IEnumerable<T> this[string partition] => GetData(partition);


        public virtual IEnumerable<T> GetData(Func<T, bool> filter = null)
        {
            var query = new TableQuery<T>();
            return ExecuteQuery("GetData", query, filter);
        }

        public async Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys)
        {
            var query = AzureStorageUtils.QueryGenerator<T>.RowKeyOnly.GetTableQuery(rowKeys);
            var result = new List<T>();

            await ExecuteQueryAsync("GetDataRowKeysOnlyAsync", query, null, chunk =>
            {
                result.AddRange(chunk);
                return Task.FromResult(0);
            });

            return result;
        }

        public IEnumerable<T> Where(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            return ExecuteQuery("Where", rangeQuery, filter);
        }

        public async Task<IEnumerable<T>> WhereAsyncc(TableQuery<T> rangeQuery, Func<T, Task<bool>> filter = null)
        {
            var result = new List<T>();
            await ExecuteQueryAsync2("WhereAsyncc", rangeQuery, filter, itm =>
            {
                result.Add(itm);
                return true;
            });

            return result;
        }

        public virtual IEnumerable<T> GetData(string partitionKey, Func<T, bool> filter = null)
        {
            var query = CompileTableQuery(partitionKey);
            return ExecuteQuery("GetData", query, filter);
        }

        public async Task<IList<T>> GetDataAsync(Func<T, bool> filter = null)
        {
            var rangeQuery = new TableQuery<T>();
            var result = new List<T>();
            await ExecuteQueryAsync("GetDataAsync", rangeQuery, filter, itms =>
            {
                result.AddRange(itms);
                return true;
            });
            return result;
        }

        public virtual async Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null)
        {
            var rangeQuery = CompileTableQuery(partition);
            var result = new List<T>();

            await ExecuteQueryAsync("GetDataAsync", rangeQuery, filter, itms =>
            {
                result.AddRange(itms);
                return true;
            });

            return result;

        }
         
        public async Task<IEnumerable<T>> WhereAsync(TableQuery<T> rangeQuery, Func<T, bool> filter = null)
        {
            var result = new List<T>();
            await ExecuteQueryAsync("WhereAsync", rangeQuery, filter, itms =>
            {
                result.AddRange(itms);
                return true;
            });
            return result;
        }

        public Task ExecuteAsync(TableQuery<T> rangeQuery, Action<IEnumerable<T>> result)
        {
            return ExecuteQueryAsync("ExecuteAsync", rangeQuery, null, itms =>
            {
                result(itms);
                return true;
            });
        }
    }
}
