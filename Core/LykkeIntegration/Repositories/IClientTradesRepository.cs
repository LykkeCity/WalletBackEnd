using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.LykkeIntegration.Repositories
{
    public interface IClientTrade : IBaseCashBlockchainOperation
    {
        string LimitOrderId { get; }
        string MarketOrderId { get; }
        double Price { get; }
    }


    public class ClientTrade : IClientTrade
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string LimitOrderId { get; set; }
        public string MarketOrderId { get; set; }
        public double Amount { get; set; }
        public string AssetId { get; set; }
        public string BlockChainHash { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public double Price { get; set; }
    }

    public interface IClientTradesRepository
    {
        Task SaveAsync(params IClientTrade[] clientTrades);
        Task<IEnumerable<IClientTrade>> GetAsync(string clientId);

        Task<IEnumerable<IClientTrade>> GetAsync(DateTime from, DateTime to);

        Task<IClientTrade> GetAsync(string clientId, string recordId);
        Task UpdateBlockChainHashAsync(string clientId, string recordId, string hash);
        Task SetBtcTransactionAsync(string clientId, string recordId, string btcTransactionId);
        Task<IEnumerable<IClientTrade>> GetByHashAsync(string blockchainHash);
    }


}