ALTER TABLE [dbo].[UnsignedTransactions] DROP CONSTRAINT [FK_UnsignedTransactions_UnsignedTransactions]
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] DROP CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions]
GO

ALTER TABLE [dbo].[UnsignedTransactions] DROP CONSTRAINT [PK_UnsignedTransactions]
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] DROP CONSTRAINT [PK_UnsignedTransactionSpentOutputs]
GO

ALTER TABLE UnsignedTransactions
DROP COLUMN id

ALTER TABLE UnsignedTransactions
DROP COLUMN TransactionIdWhichMadeThisTransactionInvalid

ALTER TABLE UnsignedTransactionSpentOutputs
DROP COLUMN UnsignedTransactionId

ALTER TABLE UnsignedTransactions
ADD id uniqueidentifier NOT NULL

ALTER TABLE UnsignedTransactions
ADD TransactionIdWhichMadeThisTransactionInvalid uniqueidentifier

ALTER TABLE UnsignedTransactionSpentOutputs
ADD UnsignedTransactionId uniqueidentifier NOT NULL

GO

ALTER TABLE [dbo].[UnsignedTransactions] ADD  CONSTRAINT [PK_UnsignedTransactions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] ADD  CONSTRAINT [PK_UnsignedTransactionSpentOutputs] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC,
	[OutputNumber] ASC,
	[UnsignedTransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

ALTER TABLE [dbo].[UnsignedTransactions]  WITH CHECK ADD  CONSTRAINT [FK_UnsignedTransactions_UnsignedTransactions] FOREIGN KEY([TransactionIdWhichMadeThisTransactionInvalid])
REFERENCES [dbo].[UnsignedTransactions] ([id])
GO

ALTER TABLE [dbo].[UnsignedTransactions] CHECK CONSTRAINT [FK_UnsignedTransactions_UnsignedTransactions]
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs]  WITH CHECK ADD  CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions] FOREIGN KEY([UnsignedTransactionId])
REFERENCES [dbo].[UnsignedTransactions] ([id])
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] CHECK CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions]
GO