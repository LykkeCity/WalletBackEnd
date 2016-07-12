/****** Object:  Table [dbo].[WholeRefund]    Script Date: 7/11/2016 6:25:04 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[WholeRefund](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[BitcoinAddress] [varchar](35) NOT NULL,
	[TransactionHex] [text] NOT NULL,
	[TransactionId] [varchar](64) NOT NULL,
	[LockTime] [datetimeoffset](7) NOT NULL,
	[CreationTime] [datetime] NOT NULL,
 CONSTRAINT [PK_WholeRefundTransaction] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/*****************************************************************************/

/****** Object:  Table [dbo].[WholeRefundSpentOutputs]    Script Date: 7/11/2016 6:25:35 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[WholeRefundSpentOutputs](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[RefundTransactionId] [bigint] NOT NULL,
	[SpentTransactionId] [varchar](64) NOT NULL,
	[SpentTransactionOutputNumber] [int] NOT NULL,
 CONSTRAINT [PK_WholeRefundSpentOutputs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[WholeRefundSpentOutputs]  WITH CHECK ADD  CONSTRAINT [FK_WholeRefundSpentOutputs_WholeRefund] FOREIGN KEY([RefundTransactionId])
REFERENCES [dbo].[WholeRefund] ([id])
GO

ALTER TABLE [dbo].[WholeRefundSpentOutputs] CHECK CONSTRAINT [FK_WholeRefundSpentOutputs_WholeRefund]
GO


/**********************************************************************/

/****** Object:  Table [dbo].[TransactionsWaitForConfirmations]    Script Date: 7/11/2016 6:27:09 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[TransactionsWaitForConfirmations](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[txToBeWatched] [varchar](64) NOT NULL,
	[WholeRefundId] [bigint] NULL,
	[OldRefundedTxId] [bigint] NULL,
 CONSTRAINT [PK_WholeRefund_RefundedOutputs] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[TransactionsWaitForConfirmations]  WITH CHECK ADD  CONSTRAINT [FK_TransactionsWaitForConfirmations_RefundTransactions] FOREIGN KEY([OldRefundedTxId])
REFERENCES [dbo].[RefundTransactions] ([id])
GO

ALTER TABLE [dbo].[TransactionsWaitForConfirmations] CHECK CONSTRAINT [FK_TransactionsWaitForConfirmations_RefundTransactions]
GO

ALTER TABLE [dbo].[TransactionsWaitForConfirmations]  WITH CHECK ADD  CONSTRAINT [FK_TransactionsWaitForConfirmations_WholeRefund] FOREIGN KEY([WholeRefundId])
REFERENCES [dbo].[WholeRefund] ([id])
GO

ALTER TABLE [dbo].[TransactionsWaitForConfirmations] CHECK CONSTRAINT [FK_TransactionsWaitForConfirmations_WholeRefund]
GO



