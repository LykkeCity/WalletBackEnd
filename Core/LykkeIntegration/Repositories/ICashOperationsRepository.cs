using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.LykkeIntegration.Repositories
{
    /// <summary>
    /// Base cash operation
    /// </summary>
    public interface IBaseCashOperation
    {
        /// <summary>
        /// Record Id
        /// </summary>
        string Id { get; }

        string AssetId { get; }

        string ClientId { get; }

        double Amount { get; }

        DateTime DateTime { get; }

        bool IsHidden { get; }
    }

    /// <summary>
    /// Base cash blockchain operation
    /// E.g. cash in, cash out, trade, transfer
    /// </summary>
    public interface IBaseCashBlockchainOperation : IBaseCashOperation
    {
        string BlockChainHash { get; }

        /// <summary>
        /// Bitcoin queue record id (BitCointTransaction)
        /// </summary>
        string TransactionId { get; }

        string AddressFrom { get; set; }

        string AddressTo { get; set; }
    }

    public interface ICashInOutOperation : IBaseCashBlockchainOperation
    {
        bool IsRefund { get; set; }
    }

    public class CashInOutOperation : ICashInOutOperation
    {
        public string Id { get; set; }
        public DateTime DateTime { get; set; }
        public bool IsHidden { get; set; }
        public string AssetId { get; set; }
        public string ClientId { get; set; }
        public double Amount { get; set; }
        public string BlockChainHash { get; set; }
        public string TransactionId { get; set; }
        public string AddressFrom { get; set; }
        public string AddressTo { get; set; }
        public bool IsRefund { get; set; }

        public static CashInOutOperation CreateNew(string assetId, double amount)
        {
            return new CashInOutOperation
            {
                DateTime = DateTime.UtcNow,
                Amount = amount,
                AssetId = assetId
            };
        }
    }

    public interface ICashOperationsRepository
    {
        Task<string> RegisterAsync(string clientId, ICashInOutOperation operation);
        Task<IEnumerable<ICashInOutOperation>> GetAsync(string clientId);
        Task<ICashInOutOperation> GetAsync(string clientId, string recordId);
        Task UpdateBlockchainHashAsync(string clientId, string id, string hash);
        Task SetBtcTransaction(string clientId, string id, string bcnTransactionId);
        Task<IEnumerable<ICashInOutOperation>> GetByHashAsync(string blockchainHash);
    }
}