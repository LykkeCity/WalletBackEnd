ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] DROP CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions]
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] DROP CONSTRAINT [PK_UnsignedTransactionSpentOutputs]
GO

ALTER TABLE UnsignedTransactionSpentOutputs ALTER COLUMN TransactionId varchar(64) NOT NULL
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] ADD  CONSTRAINT [PK_UnsignedTransactionSpentOutputs] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC,
	[OutputNumber] ASC,
	[UnsignedTransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs]  WITH CHECK ADD  CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions] FOREIGN KEY([UnsignedTransactionId])
REFERENCES [dbo].[UnsignedTransactions] ([id])
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] CHECK CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions]
GO