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
    
    public partial class WholeRefundSpentOutput
    {
        public long id { get; set; }
        public long RefundTransactionId { get; set; }
        public string SpentTransactionId { get; set; }
        public int SpentTransactionOutputNumber { get; set; }
    
        public virtual WholeRefund WholeRefund { get; set; }
    }
}
