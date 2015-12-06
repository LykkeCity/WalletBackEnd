using System.Threading.Tasks;

namespace Core
{
    public class TransactionResultModel
    {
        public string TransactionId { get; set; }
        public bool Result { get; set; }

        public static TransactionResultModel Create(string transactionId, bool result)
        {
            return new TransactionResultModel
            {
                TransactionId = transactionId,
                Result = result,
      
            };
        }
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
