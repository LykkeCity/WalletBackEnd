ALTER TABLE PreGeneratedOutput
ADD [ReservedForAddress] [varchar](35) NULL

/****** Object:  Table [dbo].[UnsignedTransactions]    Script Date: 8/8/2016 5:42:37 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[UnsignedTransactions](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[TransactionHex] [text] NULL,
	[IsClientSignatureRequired] [bit] NULL,
	[ClientSignedTransaction] [text] NULL,
	[IsExchangeSignatureRequired] [bit] NULL,
	[ExchangeSignedTransactionAfterClient] [text] NULL,
	[OwnerAddress] [varchar](35) NULL,
	[TransactionSendingSuccessful] [bit] NULL,
	[TransactionSendingError] [text] NULL,
	[TransactionId] [varchar](64) NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_UnsignedTransactions] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

/****** Object:  Table [dbo].[PregeneratedReserve]    Script Date: 8/8/2016 5:44:07 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[PregeneratedReserve](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[ReserveId] [varchar](50) NOT NULL,
	[PreGeneratedOutputTxId] [varchar](100) NOT NULL,
	[PreGeneratedOutputN] [int] NOT NULL,
	[CreationTime] [datetime] NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_PregeneratedReserve] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[PregeneratedReserve]  WITH CHECK ADD  CONSTRAINT [FK_PregeneratedReserve_PregeneratedReserve] FOREIGN KEY([PreGeneratedOutputTxId], [PreGeneratedOutputN])
REFERENCES [dbo].[PreGeneratedOutput] ([TransactionId], [OutputNumber])
GO

ALTER TABLE [dbo].[PregeneratedReserve] CHECK CONSTRAINT [FK_PregeneratedReserve_PregeneratedReserve]
GO
