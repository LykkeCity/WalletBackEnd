namespace LykkeWalletServices.Transactions.Responses
{
    public class TaskResultBase
    {
        public bool HasErrorOccurred { get; set; }
        public string ErrorMessage { get; set; }
        public int SequenceNumber { get; set; } // -1, means there will be no answers
    }
}
