USE [LykkeDev]
GO

/****** Object:  Table [dbo].[InputOutputMessageLog]    Script Date: 7/30/2016 7:52:19 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[InputOutputMessageLog](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[InputMessage] [text] NULL,
	[OutputMessage] [text] NULL,
	[CreationDate] [datetime] NULL,
 CONSTRAINT [PK_InputOutputMessageLog] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO