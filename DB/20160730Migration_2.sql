ALTER TABLE SentTransactions
ADD 
[CreationDate] [datetime] NULL,
[IsClientSignatureRequired] [bit] NULL,
[ClientSignedTransaction] [text] NULL,
[IsExchangeSignatureRequired] [bit] NULL,
[ExchangeSignedTransactionAfterClient] [text] NULL,
[TransactionId] [varchar](64) NULL,
[TransactionSendingSuccessful] [bit] NULL,
[TransactionSendingError] [text] NULL


