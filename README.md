#WalletBackEnd



## Running the project

The main project to run is ServiceLykkeWallet of the solution file.

## Implemented services up to now

All service are queue based

Currently, due to implementation decisions, in the following methods whenever private key for the local wallet is documented in the call contract, it could be totally omitted; they are present to cover the future scenarios.

When a call ends with an error, the error (in the following responses) will be a json string (instead of null) similar to "Error":{"Code":0,"Message":"..."} . The error code number could be get from ErrorCode enum.
 
*   Generate New Wallet

        Sample request: GenerateNewWallet:{"TransactionId":"10"}
        Sample response: GenerateNewWallet:{"TransactionId":"10","Result":"WalletAddress":"xxxx","WalletPrivateKey":"xxxx","MultiSigAddress":"xxx"},"Error":null}
   
*   Cash In

        Sample request: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
        Sample response: CashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Ordinary Cash In

        Sample request: OrdinaryCashIn:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
        Sample response: OrdinaryCashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Cash Out

        Sample request: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx"}
        Sample response: CashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Ordinary Cash Out

        Sample request: OrdinaryCashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx", "PublicWallet":"xxx"}
        Sample response: OrdinaryCashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   GetCurrentBalance

        Sample request: GetCurrentBalance:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B" }
        Sample response: GetCurrentBalance:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkUSD","Amount":9400.0},{"Asset":"bjkEUR","Amount":1300.0},{"Asset":"TestExchangeUSD","Amount":1300.0}]},"Error":null}

*   Swap

        Sample request: Swap:{"TransactionId":"10", MultisigCustomer1:"2N8zbehwdz2wcCd2JwZktnt6uKZ8fFMZVPp", "Amount1":200, "Asset1":"TestExchangeUSD", MultisigCustomer2:"2N8Z7FLao3qWc8h8mveDXaAA9q1Q53xMsyL", "Amount2":300, "Asset2":"TestExchangeEUR"}
        Sample response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}

*   Transfer

        Sample request: Transfer:{"SourceMultisigAddress":"2N5H4VU7R4s5CBsyq77HQ7Gu8ZXKDz3ZHVD","SourcePrivateKey":"???","DestinationMultisigAddress":"2N3e9ZNg6uFbVg7EwnSsaWPr6VAbnDfjkTo", "Amount":100, "Asset":"bjkUSD"}
        Sample response: Transfer:{"TransactionId":null,"Result":{"TransactionHex":"???","TransactionHash":"???"},"Error":null}

*   GenerateFeeOutputs

        Sample request: GenerateFeeOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.00015,"Count":1000}
        Sample request: GenerateFeeOutputs:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}

*   GenerateIssuerOutputs

        Sample request: GenerateIssuerOutputs:{"WalletAddress":"mybDLSPHeYvbvLRrKTF7xiuQ9nRKyGfFFw","PrivateKey":"???","FeeAmount":0.0000273,"Count":10, "AssetName":"bjkUSD"}
        Sample request: GenerateIssuerOutputs:{"TransactionId":null,"Result":{"TransactionHash":"xxx"},"Error":null}

*   Getting fee outputs count

        Sample request: GetFeeOutputsStatus:{"TransactionId":"10"}
        Sample response: GetFeeOutputsStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Amount":9999.0,"Count":30},{"Amount":15000.0,"Count":1000},{"Amount":10000.0,"Count":90}]},"Error":null}

*   Getting asset issuance coins

        Sample request: GetIssuersOutputStatus:{"TransactionId":"10"}
        Sample response: GetIssuersOutputStatus:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkEUR","Amount":15000.0,"Count":1000},{"Asset":"bjkUSD","Amount":2730.0,"Count":30},{"Asset":"bjkUSD","Amount":15000.0,"Count":1000}]},"Error":null}

*   Generating the refund transaction

        Sample request: GenerateRefundingTransaction:{"TransactionId":"10","MultisigAddress":"2Mvny9kSaUJhbCTMWUaFQPYERCSrduKJjD4", "timeoutInMinutes":360}
        Sample response: GenerateRefundingTransaction:{"TransactionId":"10","Result":{"RefundTransaction":"xxx"},"Error":null}

*   Getting correspondent wallet addresses

        Sample request: GetInputWalletAddresses:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Asset":"TestExchangeUSD"}
        Sample response: GetInputWalletAddresses:{"TransactionId":"10","Result":{"Addresses":["mhF3ghWGgAJxDcUC52ar5DCv56MVzpN94W","2Msbkk8AGbzrVnENqE3m8nk4n6FyTrdnNF4","2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj"]},"Error":null}

*   Updating asset definitions

        Sample request: UpdateAssets:{"TransactionId":"10","Assets": [{ "AssetId": "oDmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d", "AssetAddress": "n2JMZcG3dKuRN4c8K89TBwwwDpHshppQUr", "Name": "TestExchangeUSD", "PrivateKey": "xxx", "DefinitionUrl": "https://www.cpr.sm/-KDPVKLTlL","Divisibility": 2 }, { "AssetId": "oZTd8ZfoyRPkYFhbeLXvordpcpND2YpqPg", "AssetAddress": "n4XdhcAWoRBesY2gy5hnF6ht31rLG19kqy", "Name": "TestExchangeEUR","PrivateKey": "xxx","DefinitionUrl": "https://www.cpr.sm/SBi9SeNlyB","Divisibility": 2}]}
        Sample response: UpdateAssets:{"TransactionId":"10","Result":{"Success":true},"Error":null}


## Some notes
*   The API used to explore blockchain, is now default to QBit.Ninja (hardcoded in OpenAssetsHelper.cs, the previous code still usable); The QBit.Ninja is connected to Bitcoin Regtest mode, after a new block issued one should issue the console command "bitcoin-cli generate 1" to create a new block and then in NBitcoin.Indexer console issue the command "NBitcoin.Indexer.Console.exe --All" to index the new block and have the new transaction available for API calls.

*   In file Program.cs in line `var json = await ReadTextAsync("F:\\Lykkex\\settings.json");` correct path to json file, this is only for the debug; the release version uses the settings.json in the solution path.

*   In the settings.json, please correct the following

| Name | Defaul Value | Description |
|------|--------------|-------------|
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
|LykkeJobsUrl|Nothing|The url used by code, to post the notification for the new transaction before sending it (Like CashIn, CashOut and ...).|

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

## Mixing signatures

*   To check the method of how to mix signatures from different devices on a single transactions, please check the class LykkeWalletServices.Transactions.TaskHandlers.SrvCashOutSeparateSignaturesTask

## Development

*   If the database has changed and the edmx was regenerated, special attention should be made to rowversion columns, please check here http://stackoverflow.com/questions/12732161/how-to-automate-setting-concurrencymode-fixed-on-all-rowversion-columns and run the tool FixVersionColumnConcurrencyMode.exe (the source is available in folder WalletBackEnd\LykkeWalletServices and should be compiled with csc)

## Database

*   Database for the applicaion is currently sql server (developped with express version) and from DB folder the DDL.sql should be run to create the database