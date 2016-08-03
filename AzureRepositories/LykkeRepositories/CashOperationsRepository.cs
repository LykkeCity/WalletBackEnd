using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Core.LykkeIntegration.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.LykkeRepositories
{
    public class CashInOutOperationEntity : TableEntity, ICashInOutOperation
    {
        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        internal static string GenerateRowKey(string id)
        {
            return id;
        }


        public string Id => RowKey;
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string AssetId { get; set; }
        public string ClientId => PartitionKey;
        public double Amount { get; set; }
        public string BlockChainHash { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsRefund { get; set; }

        public static CashInOutOperationEntity Create(string clientId, ICashInOutOperation src)
        {
            return new CashInOutOperationEntity
            {
                PartitionKey = GeneratePartitionKey(clientId),
                RowKey = GenerateRowKey(Guid.NewGuid().ToString("N")),
                DateTime = src.DateTime,
                AssetId = src.AssetId,
                Amount = src.Amount,
                BlockChainHash = src.BlockChainHash,
                IsHidden = src.IsHidden,
                IsRefund = src.IsRefund,
                AddressFrom = src.AddressFrom,
                AddressTo = src.AddressTo
            };
        }
    }

    public class CashOperationsRepository : ICashOperationsRepository
    {
        private readonly INoSQLTableStorage<CashInOutOperationEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _blockChainHashIndices;

        public CashOperationsRepository(INoSQLTableStorage<CashInOutOperationEntity> tableStorage, INoSQLTableStorage<AzureIndex> blockChainHashIndices)
        {
            _tableStorage = tableStorage;
            _blockChainHashIndices = blockChainHashIndices;
        }

        public async Task<string> RegisterAsync(string clientId, ICashInOutOperation operation)
        {
            var newItem = CashInOutOperationEntity.Create(clientId, operation);
            await _tableStorage.InsertAsync(newItem);

            if (!string.IsNullOrEmpty(operation.BlockChainHash))
            {
                var indexEntity = AzureIndex.Create(operation.BlockChainHash, newItem.Id, newItem);
                await _blockChainHashIndices.InsertAsync(indexEntity);
            }

            return newItem.Id;
        }

        public async Task<IEnumerable<ICashInOutOperation>> GetAsync(string clientId)
        {
            var partitionkey = CashInOutOperationEntity.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partitionkey);
        }

        public async Task<ICashInOutOperation> GetAsync(string clientId, string recordId)
        {
            var partitionkey = CashInOutOperationEntity.GeneratePartitionKey(clientId);
            var rowKey = CashInOutOperationEntity.GenerateRowKey(recordId);
            return await _tableStorage.GetDataAsync(partitionkey, rowKey);
        }

        public async Task UpdateBlockchainHashAsync(string clientId, string id, string hash)
        {
            var partitionkey = CashInOutOperationEntity.GeneratePartitionKey(clientId);
            var rowKey = CashInOutOperationEntity.GenerateRowKey(id);

            var indexEntity = AzureIndex.Create(hash, rowKey, partitionkey, rowKey);
            await _blockChainHashIndices.InsertOrReplaceAsync(indexEntity);

            await _tableStorage.MergeAsync(partitionkey, rowKey, entity =>
            {
                entity.BlockChainHash = hash;
                return entity;
            });
        }

        public Task SetBtcTransaction(string clientId, string id, string bcnTransactionId)
        {
            var partitionkey = CashInOutOperationEntity.GeneratePartitionKey(clientId);
            var rowKey = CashInOutOperationEntity.GenerateRowKey(id);

            return _tableStorage.MergeAsync(partitionkey, rowKey, entity =>
            {
                entity.TransactionId = bcnTransactionId;
                return entity;
            });
        }

        public async Task<IEnumerable<ICashInOutOperation>> GetByHashAsync(string blockchainHash)
        {
            var indexes = await _blockChainHashIndices.GetDataAsync(blockchainHash);
            var keyValueTuples = indexes?.Select(x => new Tuple<string, string>(x.PrimaryPartitionKey, x.PrimaryRowKey));
            return await _tableStorage.GetDataAsync(keyValueTuples);
        }
    }
}