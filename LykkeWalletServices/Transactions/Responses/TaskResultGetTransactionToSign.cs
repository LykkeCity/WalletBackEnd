namespace LykkeWalletServices.Transactions.Responses
{
    public class TaskResultGetTransactionToSign : TaskResultBase
    {
        public string[] ExchangeIds { get; set; }
        public string[] Transactions { get; set; }
    }
}
