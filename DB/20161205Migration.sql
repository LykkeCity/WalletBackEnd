/****** Object:  Table [dbo].[LkeBlkChnMgrTransactionsToBeSent]    Script Date: 12/5/2016 5:47:08 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LkeBlkChnMgrTransactionsToBeSent](
	[TransactionId] [varchar](64) NOT NULL,
	[TransactionHex] [text] NOT NULL,
	[HasBeenSent] [bit] NOT NULL,
	[CreationDate] [datetime] NOT NULL,
	[ShouldBeSent] [bit] NOT NULL,
	[ReferenceNumber] [varchar](20) NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_TxBrdCstTransactionsToBeSent] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

ALTER TABLE [dbo].[LkeBlkChnMgrTransactionsToBeSent] ADD  CONSTRAINT [DF_TransactionsToBeSent_ShouldBeSent]  DEFAULT ((1)) FOR [ShouldBeSent]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'This indicates whether it should be sent or not, normally true. If for some reason (for example becoming invalid for unsigned chain problem), it becomes invalid., the flag will become false.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'LkeBlkChnMgrTransactionsToBeSent', @level2type=N'COLUMN',@level2name=N'ShouldBeSent'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'A reference used by other systems who may use this number for tracking' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'LkeBlkChnMgrTransactionsToBeSent', @level2type=N'COLUMN',@level2name=N'ReferenceNumber'
GO

/****** Object:  Table [dbo].[LkeBlkChnMgrCoin]    Script Date: 12/5/2016 5:46:32 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[LkeBlkChnMgrCoin](
	[TransactionId] [varchar](64) NOT NULL,
	[OutputNumber] [int] NOT NULL,
	[SatoshiAmount] [bigint] NOT NULL,
	[AssetAmount] [bigint] NULL,
	[AssetId] [varchar](50) NULL,
	[SpentTransactionId] [varchar](64) NULL,
	[SpentTransactionInputNumber] [int] NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_LkeBlkChnMgrCoin] PRIMARY KEY CLUSTERED 
(
	[TransactionId] ASC,
	[OutputNumber] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[TxBrdCstRequestSource]    Script Date: 12/5/2016 5:53:01 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TxBrdCstRequestSource](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SourceName] [varchar](10) NOT NULL,
 CONSTRAINT [PK_RequestSource] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[TxBrdCstRequestType]    Script Date: 12/5/2016 5:53:59 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TxBrdCstRequestType](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TypeName] [varchar](15) NOT NULL,
 CONSTRAINT [PK_RequestType] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Table [dbo].[TxBrdCstRequestDetails]    Script Date: 12/5/2016 5:54:41 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[TxBrdCstRequestDetails](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RequestType] [int] NOT NULL,
	[RequestSource] [int] NOT NULL,
	[ReguestDetails] [varchar](200) NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_RequestDetails] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[TxBrdCstRequestDetails]  WITH CHECK ADD  CONSTRAINT [FK_RequestDetails_RequestSource] FOREIGN KEY([RequestSource])
REFERENCES [dbo].[TxBrdCstRequestSource] ([Id])
GO

ALTER TABLE [dbo].[TxBrdCstRequestDetails] CHECK CONSTRAINT [FK_RequestDetails_RequestSource]
GO

ALTER TABLE [dbo].[TxBrdCstRequestDetails]  WITH CHECK ADD  CONSTRAINT [FK_RequestDetails_RequestType] FOREIGN KEY([RequestType])
REFERENCES [dbo].[TxBrdCstRequestType] ([Id])
GO

ALTER TABLE [dbo].[TxBrdCstRequestDetails] CHECK CONSTRAINT [FK_RequestDetails_RequestType]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Type of request for example swap or ordinary cashout' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TxBrdCstRequestDetails', @level2type=N'COLUMN',@level2name=N'RequestType'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Whether this request comes from queue or http' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TxBrdCstRequestDetails', @level2type=N'COLUMN',@level2name=N'RequestSource'
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Details of the request for example queue request details or http parameters' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'TxBrdCstRequestDetails', @level2type=N'COLUMN',@level2name=N'ReguestDetails'
GO

