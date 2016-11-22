using System.Threading.Tasks;

namespace Core.LykkeIntegration.Services
{
    public class HandleTxRequest
    {
        public string TransactionId
        {
            get;
            set;
        }

        public string BlockchainHash
        {
            get;
            set;
        }

        public string Operation
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("HandleTx: TransactionId: {0} , BlockchainHash: {1} , Operation: {2}",
                TransactionId, BlockchainHash, Operation);
        }
    }

    public class HandleTxError
    {
        public int ErrorCode
        {
            get;
            set;
        }

        public string ErrorMessage
        {
            get;
            set;
        }
    }

    public interface IPreBroadcastHandler
    {
        Task<HandleTxError> HandleTx(HandleTxRequest request);
    }
}
