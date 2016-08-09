//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LykkeWalletServices
{
    using System;
    using System.Collections.Generic;
    
    public partial class UnsignedTransaction
    {
        public long id { get; set; }
        public Nullable<bool> IsClientSignatureRequired { get; set; }
        public string ClientSignedTransaction { get; set; }
        public Nullable<bool> IsExchangeSignatureRequired { get; set; }
        public string TransactionHex { get; set; }
        public string OwnerAddress { get; set; }
        public byte[] Version { get; set; }
        public string ExchangeSignedTransactionAfterClient { get; set; }
        public Nullable<bool> TransactionSendingSuccessful { get; set; }
        public string TransactionSendingError { get; set; }
        public string TransactionId { get; set; }
    }
}
