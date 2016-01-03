#WalletBackEnd



## Running the project

The main project to run is ServiceLykkeWallet of the solution file.

## Implemented services up to now

All service are queue based
 
*   Generate New Wallet

        Sample input: GenerateNewWallet:{"TransactionId":"10"}
        Sample output: GenerateNewWallet:{"TransactionId":"10","Result":"WalletAddress":"xxxx","WalletPrivateKey":"xxxx","MultiSigAddress":"xxx"},"Error":null}
   
*   Cash In

        Sample input: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
        Sample output: CashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx"},"Error":null}

*   Cash Out

        Sample input: CashOut:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":"xxx"}
        Sample output: CashOut:{"TransactionId":"10","Result":{"TransactionHex":"xxxxx"},"Error":null}

*   GetCurrentBalance

        Sample input: GetCurrentBalance:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B" }
        Sample input: GetCurrentBalance:{"TransactionId":"10","Result":{"ResultArray":[{"Asset":"bjkUSD","Amount":9400.0},{"Asset":"bjkEUR","Amount":1300.0},{"Asset":"TestExchangeUSD","Amount":1300.0}]},"Error":null}

*   Swap

        Sample request: Swap:{"TransactionId":"10", MultisigCustomer1:"2N8zbehwdz2wcCd2JwZktnt6uKZ8fFMZVPp", "Amount1":200, "Asset1":"TestExchangeUSD", MultisigCustomer2:"2N8Z7FLao3qWc8h8mveDXaAA9q1Q53xMsyL", "Amount2":300, "Asset2":"TestExchangeEUR"}
        Sample response: Swap:{"TransactionId":"10","Result":{"TransactionHex":"xxxxx"},"Error":null}

## Some notes
*   In file Program.cs in line `var json = await ReadTextAsync("F:\\Lykkex\\settings.json");` correct path to json file, this is only for the debug; the release version uses the settings.json in the solution path.

*   In the settings.json, please correct the following

| Name | Defaul Value | Description |
|------|--------------|-------------|
|InQueueConnectionString|UseDevelopmentStorage=true|The connection string for the input queue, the default is for the emulator|
|OutQueueConnectionString|UseDevelopmentStorage=true|The connection string for the output queue, the default is for the emulator|
|NetworkType|TestNet|Network type, can be Main and TestNet (TestNet is included for testing)|
|exchangePrivateKey|Nothing|The private key of the exchange used for creating multi sig wallets|
|RPCUsername|Nothing|The username for the server running the bitcoind, this is the rpc username|
|RPCPassword|Nothing|The password for the server running the bitcoind, this is the rpc password|
|RPCServerIpAddress|Nothing|This is the server address for the bitcoind rpc server|
|AssetDefinitions|Nothing|The array of assets used by the exchange,consisting of various fields described in the following table.|

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

*   WalletDefinitions are not required by now.
*   In the application .config file in the `connectionStrings` section one should configure the `SqliteLykkeServicesEntities`  to point to the correct location of Sqlite database file.

