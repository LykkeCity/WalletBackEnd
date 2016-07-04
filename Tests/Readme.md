#Tests

Tests for WalletBackEnd are written using NUnit. NUnitGui could be used for executing tests.

## Running the tests

To run tests and verify they pass, following should be installed and configured properly:

*   Azure Storage Emulator: This is used to pass requests to WalletBackEnd

*   Bitcoin (Both bitcoind and bitcoin-cli is used, during test the regtest mode of bitcoind should be used)

*   QBit.Ninja (It is normally setup under IIS, and it should be configured to use what QBitNinja.Listener.Console indexes)

*   QBitNinja.Listener.Console 

*   WalletBackEnd

*   The handler for LykkeJobs callback, if required. (A dummy Http post handler is available in test folder of solution file for this purpose). This should be started manually before the tests are run.

In the folder ComponentSettings under Tests project, there are 4 sample configuration files for QBit.Ninja , Listener, WalletBackEnd and Bitcoind; they could be modified to fit the testing environment.

## Test configuration file

The App.config file for tests has the following values which should be configured:

| Name | Description |
|------|-------------|
|AzureStorageEmulatorPath|The path to Azure storage emulator to be run, to clear the tables storing the bitcoin indexed items (tests will clear all tables)|
|BitcoinDaemonPath|The path to bitcoind.exe|
|BitcoinWorkingPath|The working directory for bitcoin|
|RegtestRPCUsername|The RPC username for bitcoind|
|RegtestRPCPassword|The RPC password for bitcoind|
|RegtestRPCIP|The RPC ip address of bitcoind server|
|RegtestPort|The RPC port number for bitcoind server|
|QBitNinjaListenerConsolePath|The path QBit.Ninja Listener Console, it is used for indexing blockchain info|
|WalletBackendExecutablePath|The path to WalletBackEnd under test|
|InQueueConnectionString|The string used to connect to the queue from which the outputs of walletbackend is read (outdata).|
|OutQueueConnectionString|The string used to connect to the queue to which the inputs to walletbackend is written (indata).|
|DBConnectionString|The connection string for WalletBackEnd database, used to clear everything to reset state|
|Network|The network for test, it is TestNet|
|ExchangePrivateKey|The exchange private key, used for creating multisig during testing|
|QBitNinjaBaseUrl|The base url for QBit.Ninja, used for retrieving blockchain information|
