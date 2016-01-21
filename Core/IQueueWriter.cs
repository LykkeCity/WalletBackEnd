using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Core
{
    public class TransactionResultModel
    {
        [JsonIgnore]
        public string OperationName { get; set; }
        public string TransactionId { get; set; }
        public ITaskResult Result { get; set; }
        public Error Error { get; set; }

        public static TransactionResultModel Create(string operationName, string transactionId, ITaskResult result, Error error)
        {
            return new TransactionResultModel
            {
                OperationName = operationName,
                TransactionId = transactionId,
                Error = error,
                Result = result,

            };
        }
    }

    public interface ITaskResult
    {

    }

    public class GenerateNewWalletTaskResult : ITaskResult
    {
        public string WalletAddress { get; set; }
        public string WalletPrivateKey { get; set; }
        public string MultiSigAddress { get; set; }
    }

    public class TransactionSenderTaskResult : ITaskResult
    {
        public string TransactionHex { get; set; }
        public string TransactionHash { get; set; }
    }

    public class CashInTaskResult : TransactionSenderTaskResult
    {
    }

    public class CashOutTaskResult : TransactionSenderTaskResult
    {
    }

    public class CashOutSeparateSignaturesTaskResult : TransactionSenderTaskResult
    {
    }

    public class SwapTaskResult : TransactionSenderTaskResult
    {
    }

    public class OrdinaryCashOutTaskResult : TransactionSenderTaskResult
    {
    }
    public class OrdinaryCashInTaskResult : TransactionSenderTaskResult
    {
    }

    public class GetCurrentBalanceTaskResult : ITaskResult
    {
        public GetCurrentBalanceTaskResultElement[] ResultArray;
    }

    public class GetCurrentBalanceTaskResultElement
    {
        public string Asset { get; set; }
        public float Amount { get; set; }
    }

    /// <summary>
    /// Interface, which gives access to output queue
    /// </summary>
    public interface IQueueWriter
    {
        /// <summary>
        /// Put message to queue. This methid should always accept message and handle no connection case internally
        /// </summary>
        /// <param name="message">Message to put</param>
        /// <returns>I/O task</returns>
        Task WriteQueue(TransactionResultModel message);
    }
}
