#WalletBackEnd

## Running the project

The main project to run is ServiceLykkeWallet of the solution file.

## Implemented services up to now

All service are queue based

When a call ends with an error, the error (in the following responses) will be a json string (instead of null) similar to "Error":{"Code":0,"Message":"..."} . The error code number could be get from ErrorCode enum.
 
*   Generate New Wallet

        Sample request: GenerateNewWallet:{"TransactionId":"10"}
        Sample response: GenerateNewWallet:{"TransactionId":"10","Result":{"WalletAddress":"mtNawPk9v3QaMaF3bfTBXzc4wVdJ6YrfS9","WalletPrivateKey":"xxx","MultiSigAddress":"2N23DbiKurkz9n9nd9kLgZpnUjHiGszq4BT","ColoredWalletAddress":"bX4LUBZZVPXJGDpQQeHZNBrWeH6oU6yvT3d","ColoredMultiSigAddress":"c7C16qt9FLEsqePwzCNSsDgh44ttSqGVyBE"},"Error":null}

This has become obsolete now.
   
*   Cash In

        Sample request: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
        Sample response: CashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Ordinary Cash In

        Sample request: OrdinaryCashIn:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD", "PublicWallet":"xxx"}
        Sample response: OrdinaryCashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Cash Out

        Sample request: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD"}
        Sample response: CashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Ordinary Cash Out

        Sample request: OrdinaryCashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD", "PublicWallet":"xxx"}
        Sample response: OrdinaryCashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   GetCurrentBalance

        Sample request: GetCurrentBalance:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B", "MinimumConfirmation":0 }
        Sample response: GetCurrentBalance:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkUSD","Amount":9400.0},{"Asset":"bjkEUR","Amount":1300.0},{"Asset":"TestExchangeUSD","Amount":1300.0}]},"Error":null}

*   Swap

        Sample request: Swap:{"TransactionId":"10", MultisigCustomer1:"2N8zbehwdz2wcCd2JwZktnt6uKZ8fFMZVPp", "Amount1":200, "Asset1":"TestExchangeUSD", MultisigCustomer2:"2N8Z7FLao3qWc8h8mveDXaAA9q1Q53xMsyL", "Amount2":300, "Asset2":"TestExchangeEUR"}
        Sample response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Transfer

        Sample request: Transfer:{"TransactionId":"10","SourceAddress":"2NDT6sp172w2Hxzkcp8CUQW9bB36EYo3NFU","DestinationAddress":"2MxTYg5MsBANnTQVB88AtUmQw5zhj5gayxT", "Amount":2, "Asset":"TestExchangeUSD"}
        Sample response: Transfer:{"TransactionId":"10","Result":{"TransactionHex":"???","TransactionHash":"???"},"Error":null}

*   GenerateFeeOutputs

        Sample request: GenerateFeeOutputs:{"TransactionId":"10","WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","FeeAmount":0.00015,"Count":1000}
        Sample response: GenerateFeeOutputs:{"TransactionId":"10","Result":{"TransactionHash":"xxx"},"Error":null}

*   GenerateIssuerOutputs

        Sample request: GenerateIssuerOutputs:{"TransactionId":"10","WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","FeeAmount":0.0000273,"Count":10, "AssetName":"bjkUSD"}
        Sample response: GenerateIssuerOutputs:{"TransactionId":"10","Result":{"TransactionHash":"xxx"},"Error":null}

*   Getting fee outputs count

        Sample request: GetFeeOutputsStatus:{"TransactionId":"10"}
        Sample response: GetFeeOutputsStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Amount":9999.0,"Count":30},{"Amount":15000.0,"Count":1000},{"Amount":10000.0,"Count":90}]},"Error":null}

*   Getting asset issuance coins

        Sample request: GetIssuersOutputStatus:{"TransactionId":"10"}
        Sample response: GetIssuersOutputStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkEUR","Amount":15000.0,"Count":1000},{"Asset":"bjkUSD","Amount":2730.0,"Count":30},{"Asset":"bjkUSD","Amount":15000.0,"Count":1000}]},"Error":null}

*   Generating the refund transaction

        Sample request: GenerateRefundingTransaction:{"TransactionId":"10","MultisigAddress":"2NDT6sp172w2Hxzkcp8CUQW9bB36EYo3NFU", "RefundAddress":"mt2rMXYZNUxkpHhyUhLDgMZ4Vfb1um1XvT", "PubKey":"PubKeyInHex", "timeoutInMinutes":360 , "JustRefundTheNonRefunded":true, "FeeWillBeInsertedNow":false}
        Sample response: GenerateRefundingTransaction:{"TransactionId":"10","Result":{"RefundTransaction":"xxx"},"Error":null}

JustRefundTheNonRefunded flag indicates the old refund method should be used. If false or omitted the new refunding method will be used. Old refund method is deprecated.

FeeWillBeInsertedNow flag indicates whether fees will be inserted when creating refund or it will be left to the time when the client is signing and broadcasting the transaction. If omitted it will be considered as true.

*   Uncoloring colored transactions

        Sample request: Uncolor:{"TransactionId":"10","MultisigAddress":"2N8Uvcw6NmJKndpJw1V2qEghSHUvbrjcDPL","Amount":3,"Currency":"TestExchangeUSD"}
        Sample response: Uncolor:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Getting correspondent wallet addresses

        Sample request: GetInputWalletAddresses:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Asset":"TestExchangeUSD"}
        Sample response: GetInputWalletAddresses:{"TransactionId":"10","Result":{"Addresses":["mhF3ghWGgAJxDcUC52ar5DCv56MVzpN94W","2Msbkk8AGbzrVnENqE3m8nk4n6FyTrdnNF4","2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj"]},"Error":null}

*   Updating asset definitions

        Sample request: UpdateAssets:{"TransactionId":"10","Assets": [{ "AssetId": "oDmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d", "AssetAddress": "n2JMZcG3dKuRN4c8K89TBwwwDpHshppQUr", "Name": "TestExchangeUSD", "PrivateKey": "xxx", "DefinitionUrl": "https://www.cpr.sm/-KDPVKLTlL","Divisibility": 2 }, { "AssetId": "oZTd8ZfoyRPkYFhbeLXvordpcpND2YpqPg", "AssetAddress": "n4XdhcAWoRBesY2gy5hnF6ht31rLG19kqy", "Name": "TestExchangeEUR","PrivateKey": "xxx","DefinitionUrl": "https://www.cpr.sm/SBi9SeNlyB","Divisibility": 2}]}
        Sample response: UpdateAssets:{"TransactionId":"10","Result":{"Success":true},"Error":null}

Primary key for updating is the asset name. If a field is absent for an asset, the previous value is used. If a new asset name is provided, the asset will ne added to setting.json.

*   Get Expired Unclaimed Refunding Transactions

        Sample request: GetExpiredUnclaimedRefundingTransactions:{"TransactionId":"10","MultisigAddress":"2Mz5iEcM7VT3aaGRhKaAdRzJtRJtDKoYMsL"}
        Sample response: GetExpiredUnclaimedRefundingTransactions:{"TransactionId":"10","Result":{"Elements":[{"TxId":"xxx","TxHex":"xxx"}]},"Error":null}

If MultisigAddress is null (not passed), appropriate transactions for all addresses are returned.

*   Transfer all assets to an address

        Sample request: TransferAllAssetsToAddress:{"TransactionId":"10","SourceAddress":"xxx","DestinationAddress":"xxx"}
        Sample response: TransferAllAssetsToAddress:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

## Important Error Codes

Error code list could be found in core\error.cs. Important error code is as follows:

RaceWithRefund (14): Indicates an input spending could be in race with refund, the previous transaction spending that input needs to get 3 confirmations for this race to be considered finished.

## Some notes
*   The API used to explore blockchain, is now default to QBit.Ninja (hardcoded in OpenAssetsHelper.cs, the previous code still usable); The QBit.Ninja is connected to Bitcoin Regtest mode, after a new block issued one should issue the console command "bitcoin-cli generate 1" to create a new block and then in NBitcoin.Indexer console issue the command "NBitcoin.Indexer.Console.exe --All" to index the new block and have the new transaction available for API calls.

*   In file Program.cs in line `var json = await ReadTextAsync("F:\\Lykkex\\settings.json");` correct path to json file, this is only for the debug; the release version uses the settings.json in the solution path.

*   In the settings.json, please correct the following

| Name | Defaul Value | Description |
|------|--------------|-------------|
|RestEndPoint|Nothing|The web endpoint used to submit requests|
|InQueueConnectionString|UseDevelopmentStorage=true|The connection string for the input queue, the default is for the emulator|
|OutQueueConnectionString|UseDevelopmentStorage=true|The connection string for the output queue, the default is for the emulator|
|ConnectionString|Nothing|The connection string to sqlite database|
|NetworkType|TestNet|Network type, can be Main and TestNet (TestNet is included for testing)|
|exchangePrivateKey|Nothing|The private key of the exchange used for creating multi sig wallets|
|RPCUsername|Nothing|The username for the server running the bitcoind, this is the rpc username|
|RPCPassword|Nothing|The password for the server running the bitcoind, this is the rpc password|
|RPCServerIpAddress|Nothing|This is the server address for the bitcoind rpc server|
|AssetDefinitions|Nothing|The array of assets used by the exchange,consisting of various fields described in the following table.|
|FeeAddress|Nothing|The address which is used to send the outputs for fee generation to, this outputs will later be used to pay transaction fee.|
|FeeAddressPrivateKey|Nothing|The private key of the above address|
|QBitNinjaBaseUrl|Nothing|The qbit ninja url used for querying bitcoin network|
|PreGeneratedOutputMinimumCount|Nothing|Minimum number of pregenerated outptuts required, either for fee payment or coin issuance. When the number of pregenerated outputs fell below this number and such an output was required, an alert email would be sent (using emailsqueue)|
|DefaultNumberOfRequiredConfirmations|1|Minimum number of confirmations required to consider the transaction as final (optional).|
|SwapMinimumConfirmationNumber|0|Minimum number of confirmations required to consider the transaction as final for the swap operation (optional).|
|GenerateRefundingTransactionMinimumConfirmationNumber|1|Minimum number of confirmations required to consider the transaction as final for the generate refunding transaction operation|
|BroadcastGroup|400|The Broadcast Group used to send the email for insufficient fee outputs to (optional).|
|EnvironmentName|null|The name environment in which the program is being runned, for example test or production.|
|PrivateKeyWillBeSubmitted|false|If the private key will be submitted through POST via url [RestEndPoint]/PrivateKey/Add, if new http based method is not used; it is recommended to submit private keys, previous methods has become obsolete|
|UseSegKeysTable|true|If the SegKeys table will be used for exchange private key|
|UnsignedTransactionsUpdaterPeriod|10 minutes|The timer period for updating the unsigned transaction status and their consumed fees.|
|UnsignedTransactionTimeoutInMinutes|5 minutes|Number of minutes after which unsigned transactions are timed out.|
|IsConfigurationEncrypted|false|Wether the configuration is encrypted, if so InQueueConnectionString , OutQueueConnectionString , ConnectionString, LykkeSettingsConnectionString , exchangePrivateKey , FeeAddressPrivateKey and asset private keys are encrypted. Before component usage DecodeSettingsUsingTheProvidedPrivateKey should be called with proper key.|
|TransferFromPrivateWalletMinimumConfirmationNumber|0|The number of confirmations required to send transaction from private wallet.|
|TransferFromMultisigWalletMinimumConfirmationNumber|0|The number of confirmations required to send transaction from private wallet.|
|FeeMultiplicationFactor|1|The multiplication factor used for fee generation.|
|FeeType|HalfHourFee|21.co fee type to be used, valid values are FastestFee , HalfHourFee , HourFee|
|FeeReserveCleanerTimerPeriodInSeconds|60|Fee reserve cleaner cleans the fee reserves periodically, this is the period in seconds.|
|FeeReserveCleanerNumberOfFeesToCleanEachTime|20|Number of items cleaned each time by fee reserve cleaner.|


The AssetDefinitions is an array of json, with the following fields:

| Name | Description |
|------|-------------|
|AssetId|Id of the asset in Base 58|
|Name|Name of the asset which is used while mentioning the asset in API Call|
|PrivateKey|Private key of the asset used while issuing the asset|
|DefinitionUrl|The asset definition url, used while issuing the asset|
|Divisibility|Number of decimal places for the asset|


*   The exchange private key could be generated using TestConsole project, Program.cs, function TestBitcoinScripts, for the configured Main or TestNet.

*   For debug install latest [Microsoft Azure Storage Emulator](http://download.microsoft.com/download/0/F/E/0FE64840-9806-4D3C-9C11-84B743162618/MicrosoftAzureStorageEmulator.msi)

## Configuration encryption

*   If IsConfigurationEncrypted is enabled the configuration strings mentioned in the flag description are encrypted, and the following command should be invoked after startup (With the proper key, instead of the one provided).

*   This command should be called at the beginning to decrypt the encrypted settings:  curl -X GET "http://localhost:8989/General/DecodeSettingsUsingTheProvidedPrivateKey?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7"

*   A new key could be generated using command: curl http://localhost:8989/General/GetNewTripleDESIVKey

*   To encrypt a string: curl -X GET "http://localhost:8989/General/EncryptUsingTripleDES?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7&message=Hello\""

*   To decrypt a string: curl -X GET "http://localhost:8989/General/DecryptUsingTripleDES?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7&encrypted=9436D1DC92F8232C"

*   There is a GeneralHelper solution which could be used for encrypting/decrypting the string as a helper.


## Mixing signatures

To check the method of how to mix signatures from different devices on a single transactions, please check the class LykkeWalletServices.Transactions.TaskHandlers.SrvCashOutSeparateSignaturesTask

## Finializing refund

In order to finalize refund, there is a GeneralHelper GUI, which the tab "Refund finalizer" could be used to finalize the refund (possibly adding fees and also signing by client signature).

## Development

If the database has changed and the edmx was regenerated, special attention should be made to rowversion columns, please check here http://stackoverflow.com/questions/12732161/how-to-automate-setting-concurrencymode-fixed-on-all-rowversion-columns and run the tool FixVersionColumnConcurrencyMode.exe (the source is available in folder WalletBackEnd\LykkeWalletServices and should be compiled with csc)

## Database

Database for the applicaion is currently sql server (developped with express version) and from DB folder the DDL.sql should be run to create the database

## Tests

The details about tests is listed in the readme.md in tests folder

## HTTP Methods

Some HTTP Methods has been added to the component, they are available in the Controllers directory of ServiceLykkeWallet, method usage are available by a comment on the method, some of the methods may soon change in future.