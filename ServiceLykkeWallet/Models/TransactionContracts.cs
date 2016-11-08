namespace ServiceLykkeWallet.Models
{
    public class UnsignedTransaction
    {
        public string Id
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

        public int MinimumConfirmationNumber
        {
            get;
            set;
        }
    }

    public class SwapTransferRequest
    {
        public string MultisigCustomer1
        {
            get;
            set;
        }
        public double Amount1
        {
            get;
            set;
        }
        public string Asset1
        {
            get;
            set;
        }
        public string MultisigCustomer2
        {
            get;
            set;
        }
        public double Amount2
        {
            get;
            set;
        }
        public string Asset2
        {
            get;
            set;
        }
    }

    public class TranctionSignAndBroadcastRequest
    {
        public string Id
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

    

    public class TransactionSignResponse
    {
        public string SignedTransaction
        {
            get;
            set;
        }
    }
}
