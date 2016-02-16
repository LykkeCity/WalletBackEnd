using Core;
using LykkeWalletServices;
using LykkeWalletServices.Transactions.TaskHandlers;
using NBitcoin;
using System;
using System.IO;

namespace TestConsole
{
    class Program
    {

        private static void TestRPC()
        {
            /*
            string username = "bitcoinrpc";
            string password = "73GQrFhQNuM2rhV6cWYxDMBM4bsxD9TC6hoSZo5PSqka";
            string ip = "185.117.72.57";
            Network network = Network.TestNet;
            */
            string username = "bitcoinrpc";
            string password = "!Lykke2";
            string ip = "23.97.233.80";
            Network network = Network.TestNet;

            NBitcoin.RPC.RPCClient client = new NBitcoin.RPC.RPCClient
                (new System.Net.NetworkCredential(username, password),
                ip, network);
            int count = client.GetBlockCount();
        }
        private static void TestBitcoinScripts(string WalletAddress01PrivateKey, string WalletAddress02PrivateKey)
        {
            Key exchangeKey = new Key();


            string exchangePrivateKeyMain = new BitcoinSecret(exchangeKey, Network.Main).PrivateKey.GetWif(Network.Main).ToWif();
            string exchangePrivateKeyTest = new BitcoinSecret(exchangeKey, Network.TestNet).PrivateKey.GetWif(Network.TestNet).ToWif();

            var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { (new BitcoinSecret(WalletAddress01PrivateKey)).PubKey ,
                (new BitcoinSecret(WalletAddress02PrivateKey)).PubKey });
            var address = multiSigAddress.GetScriptAddress(Network.Main);
        }

        static void Main(string[] args)
        {
            var settings = SettingsReader.ReadAppSettins();
            // var accGenerator = new SrvAccountGenerator();

            string Asset01 = settings.AssetDefinitions[0].AssetId;
            string Asset02 = settings.AssetDefinitions[1].AssetId;

            //TestBitcoinScripts(WalletAddress01PrivateKey, WalletAddress02PrivateKey);
            TestRPC();

            /*
            // Submitting a request to create an exchange transfer, after this clients should sign the transaction
            TaskToDoGenerateExchangeTransfer exchangeRequest = new TaskToDoGenerateExchangeTransfer
            {
                Amount01 = 10,
                Amount02 = 20,
                WalletAddress01 = WalletAddress01,
                WalletAddress02 = WalletAddress02,
                Asset01 = Asset01,
                Asset02 = Asset02,
                TransactionId = Guid.NewGuid().ToString()
            };
            var t1 = SrvGenerateExchangeTransferTask.ExecuteTask(exchangeRequest);
            t1.Wait();
            var ret1 = t1.Result;

            ClientGetTransactionSignItReturnIt(WalletAddress01, WalletAddress01PrivateKey);
            ClientGetTransactionSignItReturnIt(WalletAddress02, WalletAddress02PrivateKey);
            */
            Console.ReadLine();
        }

        /*
        private static void ClientGetTransactionSignItReturnIt(string walletAddress, string walletAddressPrivateKey)
        {
            TaskToDoGetTransactionToSign getTxToSign = new TaskToDoGetTransactionToSign
            {
                TransactionId = Guid.NewGuid().ToString(),
                WalletAddress = walletAddress
            };
            var getTx = SrvGetTransactionToSignTask.ExecuteTask(getTxToSign);
            getTx.Wait();
            var getTxResult = getTx.Result;

            if (!getTxResult.HasErrorOccurred && getTxResult.ExchangeIds != null)
            {
                for (int i = 0; i < getTxResult.ExchangeIds.Length; i++)
                {
                    // First client returns the signed transaction, in 1st phase it just returns the private key of the wallet
                    TaskToDoReturnSignedTransaction c1ReturnTxData = new TaskToDoReturnSignedTransaction
                    {
                        TransactionId = Guid.NewGuid().ToString(),
                        ExchangeId = getTxResult.ExchangeIds[i],
                        WalletAddress = walletAddress,
                        SignedTransaction = walletAddressPrivateKey
                    };
                    var returnTx = SrvReturnSignedTransactionTask.ExecuteTask(c1ReturnTxData);
                    returnTx.Wait();
                    var c1ReturnTxResult = returnTx.Result;
                }
            }
        }
        */
    }

    public static class SettingsReader
    {
        public class AssetDefinition
        {
            public string AssetId { get; set; }
            public string Name { get; set; }
            public string PrivateKey { get; set; }
        }

        public class WalletDefinition
        {
            public string Address { get; set; }
            public string PrivateKey { get; set; }
        }
        public class LykkeCredentials : ILykkeCredentials
        {
            public string PublicAddress { get; set; }
            public string PrivateKey { get; set; }
            public string CcPublicAddress { get; set; }
        }

        public class TheSettings
        {
            public string RestEndPoint { get; set; }
            public string InQueueConnectionString { get; set; }
            public string OutQueueConnectionString { get; set; }

            public LykkeCredentials LykkeCredentials { get; set; }

            public AssetDefinition[] AssetDefinitions { get; set; }

            public WalletDefinition[] WalletDefinitions { get; set; }
        }

        public static TheSettings ReadAppSettins()
        {
            try
            {
                // var json = File.ReadAllText("F:\\Lykkex\\settings.json");
                // var json = File.ReadAllText("settings.json");
#if DEBUG
                var json = File.ReadAllText("F:\\Lykkex\\settings.json");
#else
                    var json = File.ReadAllText("settings.json");
#endif
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TheSettings>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading settings.json file: " + ex.Message);
                throw;
            }
        }
    }
}
