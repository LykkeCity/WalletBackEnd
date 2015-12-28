using System.Threading.Tasks;

namespace Core
{
    public class TransactionToDoBase
    {
        public string TransactionId { get; set; }
    }

    public class TaskToDoGenerateNewWallet : TransactionToDoBase
    {
    }

    public class TaskToDoCashIn : TransactionToDoBase
    {
        public string MultisigAddress { get; set; }
        // ToDo - At first we assume the currency is not divisable
        public long Amount { get; set; }
        public string Currency { get; set; }
    }

    public class TaskToDoGetCurrentBalance : TransactionToDoBase
    {
        public string MultisigAddress { get; set; }
    }

    public class TaskToDoCashOut : TransactionToDoBase
    {
        public string MultisigAddress { get; set; }
        // ToDo - At first we assume the currency is not divisable
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string PrivateKey { get; set; }
    }

    public class TaskToDoGetBalance : TransactionToDoBase
    {
        public string WalletAddress { get; set; }

        public string AssetID { get; set; }
    }

    public class TaskToDoGenerateExchangeTransfer : TransactionToDoBase
    {
        public string WalletAddress01 { get; set; }
        public string WalletAddress02 { get; set; }
        public string Asset01 { get; set; }
        public string Asset02 { get; set; }
        public int Amount01 { get; set; }
        public int Amount02 { get; set; }

    }

    public class TaskToDoGetTransactionToSign : TransactionToDoBase
    {
        public string WalletAddress { get; set; }
    }

    public class TaskToDoReturnSignedTransaction : TransactionToDoBase
    {
        public string ExchangeId { get; set; }
        public string WalletAddress { get; set; }
        public string SignedTransaction { get; set; }
    }

    #region type of messages our queue can read
    public class TaskToDoDepositWithdraw : TransactionToDoBase
    {
        public string ClientPublicAddress { get; set; }
        public string AssetId { get; set; }
        /// <summary>
        /// >0 - Deposit
        /// <0 - Withdraw
        /// </summary>
        public double Amount { get; set; }
    }

    public class TaskToDoSendAsset : TransactionToDoBase
    {
        public string PublicAddressFrom { get; set; }
        public string PublicAddressTo { get; set; }
        public string AssetId { get; set; }
        public double Amount { get; set; }
    }
    #endregion


    /// <summary>
    /// Interface, which reads input Queue
    /// </summary>
    public interface IQueueReader
    {
        /// <summary>
        /// Get Task or null
        /// </summary>
        /// <returns>
        ///  Instance of task or null, if there is not task to do
        /// </returns>
        Task<TransactionToDoBase> GetTaskToDo();
    }

}
