namespace ServiceLykkeWallet.Models
{
    public class UnsignedTransaction
    {
        public long Id
        {
            get;
            set;
        }

        public string TransactionHex
        {
            get;
            set;
        }
    }
    public class TransferRequest
    {
        public string SourceAddress
        {
            get;
            set;
        }

        public string DestinationAddress
        {
            get;
            set;
        }

        public double Amount
        {
            get;
            set;
        }

        public string Asset
        {
            get;
            set;
        }
    }

    public class TranctionSignAndBroadcastRequest
    {
        public long Id
        {
            get;
            set;
        }

        public string ClientSignedTransaction
        {
            get;
            set;
        }
    }

    public class TransactionSignRequest
    {
        public string TransactionToSign
        {
            get;
            set;
        }

        public string PrivateKey
        {
            get;
            set;
        }
    }

    public class TransactionSignResponse
    {
        public string SignedTransaction
        {
            get;
            set;
        }
    }
}
