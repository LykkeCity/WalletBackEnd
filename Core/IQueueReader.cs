using System.Threading.Tasks;

namespace Core
{
    public class TransactionToDoBase
    {
        public string TransactionId { get; set; }
    }

    #region type of messages our queue can read
    public class TaskToDoDepositWithdraw : TransactionToDoBase
    {
        public string ClientPublicAddress { get; set; }
        public string AssetId { get; set; }
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
