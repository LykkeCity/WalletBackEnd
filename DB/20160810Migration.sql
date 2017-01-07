ALTER TABLE UnsignedTransactions
ADD [CreationTime] [datetime] NULL , [HasTimedout] [bit] NULL , [TransactionIdWhichMadeThisTransactionInvalid] [bigint] NULL

ALTER TABLE [dbo].[UnsignedTransactions]  WITH CHECK ADD  CONSTRAINT [FK_UnsignedTransactions_UnsignedTransactions] FOREIGN KEY([TransactionIdWhichMadeThisTransactionInvalid])
REFERENCES [dbo].[UnsignedTransactions] ([id])
GO

ALTER TABLE [dbo].[UnsignedTransactions] CHECK CONSTRAINT [FK_UnsignedTransactions_UnsignedTransactions]
GO


/****** Object:  Table [dbo].[UnsignedTransactionSpentOutputs]    Script Date: 8/10/2016 7:52:42 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[UnsignedTransactionSpentOutputs](
	[TransactionId] [varchar](35) NOT NULL,
	[OutputNumber] [int] NOT NULL,
	[UnsignedTransactionId] [bigint] NOT NULL,
 CONSTRAINT [PK_UnsignedTransactionSpentOutputs] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC,
	[OutputNumber] ASC,
	[UnsignedTransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs]  WITH CHECK ADD  CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions] FOREIGN KEY([UnsignedTransactionId])
REFERENCES [dbo].[UnsignedTransactions] ([id])
GO

ALTER TABLE [dbo].[UnsignedTransactionSpentOutputs] CHECK CONSTRAINT [FK_UnsignedTransactionSpentOutputs_UnsignedTransactions]
GO
