# WalletBackEnd
Walet BackEnd Solution

For debug install lastest Azure Storage Emulator: http://download.microsoft.com/download/0/F/E/0FE64840-9806-4D3C-9C11-84B743162618/MicrosoftAzureStorageEmulator.msi

Actual required implementation for generating a new wallet added:

1- The main project to run is ServiceLykkeWallet of the solution file.
2- The Service to generate a new wallet is: SrvGenerateNewWalletTask it is queue based
	Sample input: GenerateNewWallet:{"TransactionId":"10"}
	Sample output: {"TransactionId":"10","Result":{"WalletAddress":"xxxx","WalletPrivateKey":"xxxx","MultiSigAddress":"xxx"},"Error":null}
3- In file Program.cs in line "var json = await ReadTextAsync("F:\\Lykkex\\settings.json");" correct path to json file.
4- In the settings.json correct the 
	"NetworkType": "TestNet",
	 "exchangePrivateKey": "xxxx"

network type can be Main and TestNet (TestNet is included for testing)
the exchange private key could be generated using TestConsole project, Program.cs, function TestBitcoinScripts, for the configured Main or TestNet.

AssetDefinitions and WalletDefinitions are not required by now.