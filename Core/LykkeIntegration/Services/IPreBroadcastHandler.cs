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
