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
    public class TransferEventEntity : TableEntity, ITransferEvent
    {
        public string Id => RowKey;
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string FromId { get; set; }
        public string AssetId { get; set; }
        public double Amount { get; set; }
        public string BlockChainHash { get; set; }
        public string Multisig { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public string ClientId { get; set; }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string id)
            {
                return id;
            }

            public static TransferEventEntity Create(ITransferEvent src)
            {
                return new TransferEventEntity
                {
                    PartitionKey = GeneratePartitionKey(src.ClientId),
                    DateTime = src.DateTime,
                    Amount = src.Amount,
                    AssetId = src.AssetId,
                    FromId = src.FromId,
                    BlockChainHash = src.BlockChainHash,
                    TransactionId = src.TransactionId,
                    IsHidden = src.IsHidden,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig,
                    ClientId = src.ClientId
                };
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey(string multisig)
            {
                return multisig;
            }

            public static string GenerateRowKey(string id)
            {
                return id;
            }

            public static TransferEventEntity Create(ITransferEvent src, string id)
            {
                return new TransferEventEntity
                {
                    PartitionKey = GeneratePartitionKey(src.Multisig),
                    RowKey = id,
                    DateTime = src.DateTime,
                    Amount = src.Amount,
                    AssetId = src.AssetId,
                    FromId = src.FromId,
                    BlockChainHash = src.BlockChainHash,
                    TransactionId = src.TransactionId,
                    IsHidden = src.IsHidden,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig,
                    ClientId = src.ClientId
                };
            }
        }
    }

    public class TransferEventsRepository : ITransferEventsRepository
    {
        private readonly INoSQLTableStorage<TransferEventEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _blockChainHashIndices;

        public TransferEventsRepository(INoSQLTableStorage<TransferEventEntity> tableStorage, INoSQLTableStorage<AzureIndex> blockChainHashIndices)
        {
            _tableStorage = tableStorage;
            _blockChainHashIndices = blockChainHashIndices;
        }

        public async Task<ITransferEvent> RegisterAsync(ITransferEvent src)
        {
            var newEntity = TransferEventEntity.ByClientId.Create(src);
            var inserted = await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(newEntity, newEntity.DateTime,
                    RowKeyDateTimeFormat.Short);

            var byMultisigEntity = TransferEventEntity.ByMultisig.Create(src, inserted.RowKey);
            await _tableStorage.InsertAsync(byMultisigEntity);

            return newEntity;
        }

        public async Task<IEnumerable<ITransferEvent>> GetAsync(string clientId)
        {
            var partitionKey = TransferEventEntity.ByClientId.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<ITransferEvent> GetAsync(string clientId, string id)
        {
            var partitionKey = TransferEventEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = TransferEventEntity.ByClientId.GeneratePartitionKey(id);
            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task UpdateBlockChainHashAsync(string clientId, string id, string blockChainHash)
        {
            var partitionKey = TransferEventEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = TransferEventEntity.ByClientId.GenerateRowKey(id);

            var item = await _tableStorage.GetDataAsync(partitionKey, rowKey);
            item.BlockChainHash = blockChainHash;

            var multisigPartitionKey = TransferEventEntity.ByMultisig.GeneratePartitionKey(item.Multisig);
            var multisigRowKey = TransferEventEntity.ByMultisig.GenerateRowKey(id);

            var multisigItem = await _tableStorage.GetDataAsync(multisigPartitionKey, multisigRowKey);
            multisigItem.BlockChainHash = blockChainHash;

            var indexEntity = AzureIndex.Create(blockChainHash, rowKey, partitionKey, rowKey);
            await _blockChainHashIndices.InsertOrReplaceAsync(indexEntity);

            await _tableStorage.InsertOrReplaceAsync(item);
            await _tableStorage.InsertOrReplaceAsync(multisigItem);
        }

        public async Task SetBtcTransactionAsync(string clientId, string id, string btcTransactionId)
        {
            var partitionKey = TransferEventEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = TransferEventEntity.ByClientId.GenerateRowKey(id);

            var item = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = TransferEventEntity.ByMultisig.GeneratePartitionKey(item.Multisig);
            var multisigRowKey = TransferEventEntity.ByMultisig.GenerateRowKey(id);

            var multisigItem = await _tableStorage.GetDataAsync(multisigPartitionKey, multisigRowKey);

            multisigItem.TransactionId = btcTransactionId;
            item.TransactionId = btcTransactionId;

            await _tableStorage.InsertOrReplaceAsync(item);
            await _tableStorage.InsertOrReplaceAsync(multisigItem);
        }

        public async Task<IEnumerable<ITransferEvent>> GetByHashAsync(string blockchainHash)
        {
            var indexes = await _blockChainHashIndices.GetDataAsync(blockchainHash);
            var keyValueTuples = indexes?.Select(x => new Tuple<string, string>(x.PrimaryPartitionKey, x.PrimaryRowKey));
            return await _tableStorage.GetDataAsync(keyValueTuples);
        }

        public async Task<IEnumerable<ITransferEvent>> GetByMultisigAsync(string multisig)
        {
            var partitionKey = TransferEventEntity.ByMultisig.GeneratePartitionKey(multisig);
            return await _tableStorage.GetDataAsync(partitionKey);
        }
    }
}
