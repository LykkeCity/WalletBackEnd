using System;
using System.Threading.Tasks;
using Common;

namespace Core.LykkeIntegration.Repositories
{
    public class BitCoinCommands
    {
        public const string GenerateWallet = "GenerateWallet";
        public const string CashIn = "CashIn";
        public const string OrdinaryCashOut = "OrdinaryCashOut";

        public const string Swap = "Swap";
        public const string CashOut = "CashOut";
        public const string Transfer = "Transfer";
        public const string Refund = "Refund";
    }

    public interface IBitcoinTransaction
    {
        string TransactionId { get; }
        DateTime Created { get; }
        DateTime? ResponseDateTime { get; }
        string RequestData { get; }
        string ResponseData { get; }
        string ContextData { get; }
        string BlockchainHash { get; }
    }

    public interface IBitCoinTransactionsRepository
    {
        Task CreateAsync(string transactionId, string requestData, string contextData);
        Task<IBitcoinTransaction> FindByTransactionIdAsync(string transactionId);
        Task<IBitcoinTransaction> SaveResponseAndHashAsync(string transactionId, string resp, string hash, DateTime? dateTime = null);
        Task UpdateRequestAsync(string transactionId, string request);
    }


    public static class BintCoinTransactionsRepositoryExt
    {
        public static T GetContextData<T>(this IBitcoinTransaction src)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(src.ContextData);
        }

        public static Task CreateAsync(this IBitCoinTransactionsRepository src, string transactionId, string requestData, object contextData)
        {
            return src.CreateAsync(transactionId, requestData, contextData.ToJson());
        }
    }
}