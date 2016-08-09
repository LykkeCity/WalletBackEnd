using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables.Templates.Index;
using Common;
using Core.LykkeIntegration.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureRepositories.LykkeRepositories
{

    public class ClientTradeEntity : TableEntity, IClientTrade
    {
        public string Id => RowKey;

        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
        public double Price { get; set; }
        public double Amount => Volume;
        public string AssetId { get; set; }
        public string BlockChainHash { get; set; }
        public string Multisig { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public double Volume { get; set; }
        public string ClientId { get; set; }

        public static class ByClientId
        {
            public static string GeneratePartitionKey(string clientId)
            {
                return clientId;
            }

            public static string GenerateRowKey(string tradeId)
            {
                return tradeId;
            }

            public static ClientTradeEntity Create(IClientTrade src)
            {
                return new ClientTradeEntity
                {
                    PartitionKey = GeneratePartitionKey(src.ClientId),
                    RowKey = GenerateRowKey(src.Id ?? Guid.NewGuid().ToString("N")),
                    ClientId = src.ClientId,
                    AssetId = src.AssetId,
                    DateTime = src.DateTime,
                    LimitOrderId = src.LimitOrderId,
                    MarketOrderId = src.MarketOrderId,
                    Volume = src.Amount,
                    BlockChainHash = src.BlockChainHash,
                    Price = src.Price,
                    IsHidden = src.IsHidden,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig
                };
            }
        }

        public static class ByMultisig
        {
            public static string GeneratePartitionKey(string multisig)
            {
                return multisig;
            }

            public static string GenerateRowKey(string tradeId)
            {
                return tradeId;
            }

            public static ClientTradeEntity Create(IClientTrade src)
            {
                return new ClientTradeEntity
                {
                    PartitionKey = GeneratePartitionKey(src.Multisig),
                    RowKey = GenerateRowKey(src.Id ?? Guid.NewGuid().ToString("N")),
                    ClientId = src.ClientId,
                    AssetId = src.AssetId,
                    DateTime = src.DateTime,
                    LimitOrderId = src.LimitOrderId,
                    MarketOrderId = src.MarketOrderId,
                    Volume = src.Amount,
                    BlockChainHash = src.BlockChainHash,
                    Price = src.Price,
                    IsHidden = src.IsHidden,
                    AddressFrom = src.AddressFrom,
                    AddressTo = src.AddressTo,
                    Multisig = src.Multisig
                };
            }
        }
    }

    public class ClientTradesRepository : IClientTradesRepository
    {
        private readonly INoSQLTableStorage<ClientTradeEntity> _tableStorage;
        private readonly INoSQLTableStorage<AzureIndex> _blockChainHashIndices;

        public ClientTradesRepository(INoSQLTableStorage<ClientTradeEntity> tableStorage, INoSQLTableStorage<AzureIndex> blockChainHashIndices)
        {
            _tableStorage = tableStorage;
            _blockChainHashIndices = blockChainHashIndices;
        }

        [Obsolete ("Trades are created on ME side now.")]
        public async Task SaveAsync(params IClientTrade[] clientTrades)
        {
            foreach (var clientTradeBunch in clientTrades.ToPieces(10))
            {
                var clientTradeBunchArr = clientTradeBunch.ToArray();
                await
                    Task.WhenAll(
                        clientTradeBunchArr.Select(
                            clientTrade => _tableStorage.InsertOrReplaceAsync(ClientTradeEntity.ByClientId.Create(clientTrade)))
                            .Concat(clientTradeBunchArr.Select(
                            clientTrade => _tableStorage.InsertOrReplaceAsync(ClientTradeEntity.ByMultisig.Create(clientTrade))))
                       );
            }
        }

        public async Task<IEnumerable<IClientTrade>> GetAsync(string clientId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            return await _tableStorage.GetDataAsync(partitionKey);
        }

        public async Task<IEnumerable<IClientTrade>> GetAsync(DateTime @from, DateTime to)
        {
            // ToDo - Have to optimize according to the task: https://lykkex.atlassian.net/browse/LWDEV-131
            return await _tableStorage.GetDataAsync(itm => @from <= itm.DateTime && itm.DateTime < to);
        }

        public async Task<IClientTrade> GetAsync(string clientId, string recordId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            return await _tableStorage.GetDataAsync(partitionKey, rowKey);
        }

        public async Task UpdateBlockChainHashAsync(string clientId, string recordId, string hash)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(recordId);

            var indexEntity = AzureIndex.Create(hash, rowKey, partitionKey, rowKey);
            await _blockChainHashIndices.InsertOrReplaceAsync(indexEntity);

            await _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
            {
                entity.BlockChainHash = hash;
                return entity;
            });

            await _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
            {
                entity.BlockChainHash = hash;
                return entity;
            });
        }

        public async Task SetBtcTransactionAsync(string clientId, string recordId, string btcTransactionId)
        {
            var partitionKey = ClientTradeEntity.ByClientId.GeneratePartitionKey(clientId);
            var rowKey = ClientTradeEntity.ByClientId.GenerateRowKey(recordId);

            var clientIdRecord = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            var multisigPartitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(clientIdRecord.Multisig);
            var multisigRowKey = ClientTradeEntity.ByMultisig.GenerateRowKey(recordId);

            await _tableStorage.MergeAsync(partitionKey, rowKey, entity =>
            {
                entity.TransactionId = btcTransactionId;
                return entity;
            });

            await _tableStorage.MergeAsync(multisigPartitionKey, multisigRowKey, entity =>
            {
                entity.TransactionId = btcTransactionId;
                return entity;
            });
        }

        public async Task<IEnumerable<IClientTrade>> GetByHashAsync(string blockchainHash)
        {
            var indexes = await _blockChainHashIndices.GetDataAsync(blockchainHash);
            var keyValueTuples = indexes?.Select(x => new Tuple<string, string>(x.PrimaryPartitionKey, x.PrimaryRowKey));
            return await _tableStorage.GetDataAsync(keyValueTuples);
        }

        public async Task<IEnumerable<IClientTrade>> GetByMultisigAsync(string multisig)
        {
            var partitionKey = ClientTradeEntity.ByMultisig.GeneratePartitionKey(multisig);
            return await _tableStorage.GetDataAsync(partitionKey);
        }
    }
}