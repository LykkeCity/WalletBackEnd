Create Database Lykke;
go

ALTER DATABASE Lykke SET READ_COMMITTED_SNAPSHOT ON
/* ALTER DATABASE Lykke SET ALLOW_SNAPSHOT_ISOLATION ON  */

use Lykke;

CREATE TABLE ExchangeRequests (
	WalletAddress01	varchar(100),
	WalletAddress02	varchar(100),
	Asset01	varchar(100),
	Asset02	varchar(100),
	Amount01	INTEGER,
	Amount02	INTEGER,
	ServiceTransactionId	varchar(100),
	ExchangeId	varchar(100) NOT NULL,
	FirstClientSigned	INTEGER DEFAULT 0,
	SecondClientSigned	INTEGER DEFAULT 0,
	Version rowversion,
	PRIMARY KEY(ExchangeId)
);

CREATE TABLE PreGeneratedOutput (
	TransactionId	varchar(100) NOT NULL,
	OutputNumber	INTEGER NOT NULL,
	Amount	BIGINT NOT NULL,
	PrivateKey	varchar(100) NOT NULL,
	Consumed	INTEGER NOT NULL,
	Script	varchar(1000) NOT NULL,
	AssetId      varchar(100),
	Address	varchar(100),
	Network	varchar(10),
	Version rowversion,
	PRIMARY KEY(TransactionId,OutputNumber)
);

CREATE TABLE KeyStorage (
	WalletAddress	varchar(100) NOT NULL,
	WalletPrivateKey	varchar(100) NOT NULL,
	MultiSigAddress	varchar(100) NOT NULL,
	MultiSigScript	varchar(1000) NOT NULL,
	ExchangePrivateKey	varchar(100) NOT NULL,
	Network	varchar(100) NOT NULL,
	Version rowversion,
	PRIMARY KEY(WalletAddress)
);

CREATE TABLE SentTransactions (
	id	INTEGER IDENTITY(1,1) PRIMARY KEY,
	TransactionHex	text,
	Version rowversion
);

CREATE TABLE SpentOutputs (
	PrevHash	varchar(100),
	OutputNumber	INTEGER,
	SentTransactionId	INTEGER,
	Version rowversion,
	PRIMARY KEY(PrevHash,OutputNumber)
);

CREATE TABLE TransactionsToBeSigned (
	ExchangeId	varchar(100),
	WalletAddress	varchar(100),
	UnsignedTransaction	varchar(100),
	SignedTransaction	varchar(100),
	Version rowversion,
	PRIMARY KEY(ExchangeId,WalletAddress)
);

ALTER TABLE [dbo].[SpentOutputs]  WITH CHECK ADD  CONSTRAINT [FK_SpentOutputs_SentTransactions] FOREIGN KEY([SentTransactionId]) REFERENCES [dbo].[SentTransactions] ([id])