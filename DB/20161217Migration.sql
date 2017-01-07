/****** Object:  Table [dbo].[OffchainChannel]    Script Date: 12/17/2016 1:46:25 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[OffchainChannel](
	[ChannelId] [bigint] IDENTITY(1,1) NOT NULL,
	[ReplacedBy] [bigint] NULL,
	[unsignedTransactionHash] [varchar](64) NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_OffchainChannel] PRIMARY KEY CLUSTERED 
(
	[ChannelId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[OffchainChannel]  WITH CHECK ADD  CONSTRAINT [FK_OffchainChannel_OffchainChannel] FOREIGN KEY([ReplacedBy])
REFERENCES [dbo].[OffchainChannel] ([ChannelId])
GO

ALTER TABLE [dbo].[OffchainChannel] CHECK CONSTRAINT [FK_OffchainChannel_OffchainChannel]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'This table specifies the chaannel properties, at the start it just specifies which channel replaced the previous one, later it may be extended.' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'OffchainChannel'
GO

/****** Object:  Table [dbo].[MultisigChannel]    Script Date: 12/17/2016 1:47:21 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[MultisigChannel](
	[MultisigAddress] [varchar](35) NOT NULL,
	[ChannelId] [bigint] NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_MultisigChannel] PRIMARY KEY CLUSTERED 
(
	[MultisigAddress] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[MultisigChannel]  WITH CHECK ADD  CONSTRAINT [FK_MultisigChannel_OffchainChannel] FOREIGN KEY([ChannelId])
REFERENCES [dbo].[OffchainChannel] ([ChannelId])
GO

ALTER TABLE [dbo].[MultisigChannel] CHECK CONSTRAINT [FK_MultisigChannel_OffchainChannel]
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'Since each multisig may have uncompleted channel setups and could be closed and extended, a reference update would be a proper solution' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'MultisigChannel'
GO


/****** Object:  Table [dbo].[ChannelCoin]    Script Date: 12/17/2016 1:45:45 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[ChannelCoin](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[TransactionId] [varchar](64) NOT NULL,
	[OutputNumber] [int] NOT NULL,
	[ReservedForChannel] [bigint] NOT NULL,
	[ReservationFinalized] [bit] NULL,
	[ReservationTimedout] [bit] NULL,
	[ReservationCreationDate] [datetime] NOT NULL,
	[ReservedForMultisig] [nchar](35) NOT NULL,
	[ReservationEndDate] [datetime] NOT NULL,
	[Version] [timestamp] NOT NULL,
 CONSTRAINT [PK_ChannelCoin] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[ChannelCoin]  WITH CHECK ADD  CONSTRAINT [FK_ChannelCoin_OffchainChannel] FOREIGN KEY([ReservedForChannel])
REFERENCES [dbo].[OffchainChannel] ([ChannelId])
GO

ALTER TABLE [dbo].[ChannelCoin] CHECK CONSTRAINT [FK_ChannelCoin_OffchainChannel]
GO
