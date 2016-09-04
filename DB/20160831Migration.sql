ALTER TABLE UnsignedTransactions DROP COLUMN OwnerAddress

ALTER TABLE UnsignedTransactions ADD OwnerAddress varchar(70)

ALTER TABLE PreGeneratedOutput ALTER COLUMN ReservedForAddress varchar(70)

GO