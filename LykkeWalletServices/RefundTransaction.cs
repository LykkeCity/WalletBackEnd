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
    
    public partial class RefundTransaction
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RefundTransaction()
        {
            this.RefundedOutputs = new HashSet<RefundedOutput>();
            this.TransactionsWaitForConfirmations = new HashSet<TransactionsWaitForConfirmation>();
        }
    
        public long id { get; set; }
        public string RefundTxId { get; set; }
        public byte[] Version { get; set; }
        public string RefundTxHex { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<RefundedOutput> RefundedOutputs { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<TransactionsWaitForConfirmation> TransactionsWaitForConfirmations { get; set; }
    }
}
