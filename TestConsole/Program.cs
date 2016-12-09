using Core;
using LykkeWalletServices;
using LykkeWalletServices.BlockchainManager;
using LykkeWalletServices.Transactions.TaskHandlers;
using NBitcoin;
using System;
using System.IO;
using System.Threading.Tasks;
using static TestConsole.SettingsReader;

namespace TestConsole
{
    class Program
    {
        private static async Task LykkeBlockchainManagerTest(TheSettings settings)
        {
            var transactionHex = "01000000043f38b766d969b1300f4a888492b01542956bfe23ff3280c33e16a151b3033e2501000000da004730440220519dc405cfae8a39866b6277058b5a30065cc37f75d86e3fc178cbebd7294c6e02205c9dbf820ac83297d42d71c82467250f1e63c6b781d437db613fdbfd9c1dc22601483045022100c07c7a9c0872b2ab15993967899f9c4bead4ee4ce59f3aa704b25210d5fc4d6302207c2d479bba43f32580124bf6f29cbfbd5586b385e09d20b06a5e6f21c1b8f73801475221023c1d33383cec98c2127207e291945e4d6cdc909efc96657ca6db62793e688085210388f573e660292c0307a2d0ba30232a8b044c30cda60efc218a4a9cd9079155b452aefffffffff38710aa757b358713b772927079eea6c2f596aeb77f86cbdaf4817fc2adf08400000000da00483045022100aaa17d3abd75e70c91f56414ba634cdc13a70709111a267e418e1647c0697f6902201d746caaf951b0fcfa41804c45e819ca4429fbf93963393f1cc6851993426f530147304402201bde462e7b61b7d2964d59fd9831f9a947023a4dc0d326c655bdff8848ebf801022068c173645531c25f4af03b0b75fdb4a66ef1824570562fd74c767defde560b7801475221023c1d33383cec98c2127207e291945e4d6cdc909efc96657ca6db62793e688085210388f573e660292c0307a2d0ba30232a8b044c30cda60efc218a4a9cd9079155b452aeffffffff0d27b8fdb51a1960e7a38a9aae69f603789af89dff75c04a622a016e6d9ddff700000000db004830450221009fe8b97cdd6b62f67ec68ae2bb7866c013f818cfbe60d3e285c709b75a31267e022027942ec54efc49cf481f76c6e0ba15010b0760f39e271958eb6d3b94505160c901483045022100f72f373d66f47fe9e7c87a78dddd5cbafdf336dbf0a55ce1f5ed2db71784321002206131746cf6f24e7af093161599a27d78a7b0354e3498bfc6695e19dc12c6038001475221023c1d33383cec98c2127207e291945e4d6cdc909efc96657ca6db62793e688085210388f573e660292c0307a2d0ba30232a8b044c30cda60efc218a4a9cd9079155b452aeffffffff3f38b766d969b1300f4a888492b01542956bfe23ff3280c33e16a151b3033e2503000000db0048304502210080e336043e617e81eb0cd95ba839a51dab641ef08b7c80aaae37ead1a1245ab00220149f5defc41b59e67fe8a4c4ba693264d76322842087b1bb565eda772bfd244401483045022100cf2b43fb3359fd7a85e8dc88423fcf0bcb6b99c777d76fe9b633b87b611bcf7b02203d86620e37ae2eafe30d2212b37e3e88b194e8b5680ca7c6ac886e9f2af2aa5601475221023c1d33383cec98c2127207e291945e4d6cdc909efc96657ca6db62793e688085210388f573e660292c0307a2d0ba30232a8b044c30cda60efc218a4a9cd9079155b452aeffffffff0300000000000000000a6a084f41010001b05400aa0a0000000000001976a9141be72ed95c2d30a43ee44ca0bc67617a544bd2dd88acee4201000000000017a914cf65abfdd2efd1115e5551b1ac8ae835e9b58aa88700000000";
            LykkeBitcoinBlockchainManagerSettings.ConnectionString = settings.ConnectionString;
            LykkeBitcoinBlockchainManagerSettings.Network = Network.TestNet;
            LykkeBitcoinBlockchainManager.QBitNinjaBaseUrl = settings.QBitNinjaBaseUrl;
            var result = await LykkeBitcoinBlockchainManager.BroadcastTransaction(transactionHex);
        }
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

            var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { (new BitcoinSecret(WalletAddress01PrivateKey)).PubKey ,
                (new BitcoinSecret(WalletAddress02PrivateKey)).PubKey });
            var address = multiSigAddress.GetScriptAddress(Network.Main);
        }

        static void Main(string[] args)
        {
            var settings = SettingsReader.ReadAppSettins();
            // var accGenerator = new SrvAccountGenerator();
            Task.Run(async () => await LykkeBlockchainManagerTest(settings)).Wait();
            /*
            string Asset01 = settings.AssetDefinitions[0].AssetId;
            string Asset02 = settings.AssetDefinitions[1].AssetId;

            //TestBitcoinScripts(WalletAddress01PrivateKey, WalletAddress02PrivateKey);
            TestRPC();
            */
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
            public string ConnectionString { get; set; }
            public string QBitNinjaBaseUrl { get; set; }
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
                var json = File.ReadAllText("D:\\Lykkex\\settings.json");
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
