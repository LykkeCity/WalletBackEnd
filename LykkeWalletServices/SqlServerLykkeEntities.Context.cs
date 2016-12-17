﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class SqlexpressLykkeEntities : DbContext
    {
        public SqlexpressLykkeEntities()
            : base("name=SqlexpressLykkeEntities")
        {
        }
    
    	public SqlexpressLykkeEntities(string connectionString)
            : base(connectionString)
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<ExchangeRequest> ExchangeRequests { get; set; }
        public virtual DbSet<KeyStorage> KeyStorages { get; set; }
        public virtual DbSet<PreGeneratedOutput> PreGeneratedOutputs { get; set; }
        public virtual DbSet<SpentOutput> SpentOutputs { get; set; }
        public virtual DbSet<RefundedOutput> RefundedOutputs { get; set; }
        public virtual DbSet<RefundTransaction> RefundTransactions { get; set; }
        public virtual DbSet<EmailMessage> EmailMessages { get; set; }
        public virtual DbSet<WholeRefundSpentOutput> WholeRefundSpentOutputs { get; set; }
        public virtual DbSet<WholeRefund> WholeRefunds { get; set; }
        public virtual DbSet<TransactionsWaitForConfirmation> TransactionsWaitForConfirmations { get; set; }
        public virtual DbSet<InputOutputMessageLog> InputOutputMessageLogs { get; set; }
        public virtual DbSet<SentTransaction> SentTransactions { get; set; }
        public virtual DbSet<PregeneratedReserve> PregeneratedReserves { get; set; }
        public virtual DbSet<TransactionsToBeSigned> TransactionsToBeSigneds { get; set; }
        public virtual DbSet<SegKey> SegKeys { get; set; }
        public virtual DbSet<UnsignedTransaction> UnsignedTransactions { get; set; }
        public virtual DbSet<UnsignedTransactionSpentOutput> UnsignedTransactionSpentOutputs { get; set; }
        public virtual DbSet<DBLog> DBLogs { get; set; }
        public virtual DbSet<ChannelPreGeneratedOutput> ChannelPreGeneratedOutputs { get; set; }
        public virtual DbSet<ChannelCoin> ChannelCoins { get; set; }
        public virtual DbSet<MultisigChannel> MultisigChannels { get; set; }
        public virtual DbSet<OffchainChannel> OffchainChannels { get; set; }
    }
}
