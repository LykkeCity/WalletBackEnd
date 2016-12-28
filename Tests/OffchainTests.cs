using LykkeWalletServices;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;

namespace Lykkex.WalletBackend.Tests
{
    [TestFixture]
    public class OffchainTests : TaskTestsCommon
    {
        private static string[] reservedPrivateKey = new string[] {
            "cQMqC1Vqyi6o62wE1Z1ZeWDbMCkRDZW5dMPJz8QT9uMKQaMZa8JY",
            "cQyt2zxAS2uV7HJWR9hf16pFDTye8YsGL6hzd9pQzMoo9m24RGoV",
            "cSFbgd8zKDSCDHgGocccngyVSfGZsyZFiTXtimTonHyL44gTKTNU",  // 03eb5b1a93a77d6743bd4657614d87f4d2d40566558d4c8faab188d957c32c1976
            "cPBtsvLrD3DnbdGgDZ2EMbZnQurzBVmgmejiMv55jH9JehPDn5Aq"   // 035441d55de4f28fcb967472a1f9790ecfea9a9a2a92e301646d52cb3290b9e355
        };

        private static async Task<UnsignedClientCommitmentTransactionResponse> GetOffchainSignedSetup
            (string[] privateKeys)
        {
            var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
            var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
            var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
            var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

            var multisig = GetMultiSigFromTwoPubKeys(clientPrivateKey.PubKey.ToString(),
                hubPrivateKey.PubKey.ToString());

            var assetName = "TestExchangeUSD";
            await CashinToAddress(clientPrivateKey.GetAddress().ToString(), assetName, 100);
            await CashinToAddress(hubPrivateKey.GetAddress().ToString(), assetName, 100);
            await CashinToAddress(multisig.MultiSigAddress, assetName, 85);

            using (HttpClient client = new HttpClient())
            {
                string url = string.Format("{0}/Offchain/GenerateUnsignedChannelSetupTransaction?clientPubkey={1}&clientContributedAmount={2}&hubPubkey={3}&hubContributedAmount={4}&channelAssetName={5}&channelTimeoutInMinutes={6}",
                    Settings.WalletBackendUrl, clientPrivateKey.PubKey, 10, hubPrivateKey.PubKey, 10, "TestExchangeUSD", 7);
                var response = await client.GetStringAsync(url);
                UnsignedChannelSetupTransaction resp =
                    JsonConvert.DeserializeObject<UnsignedChannelSetupTransaction>(response);

                var clientSignedTx = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = resp.UnsigndTransaction,
                    PrivateKey = clientPrivateKey.ToString()
                });

                url = string.Format("{0}/Offchain/CreateUnsignedClientCommitmentTransaction?UnsignedChannelSetupTransaction={1}&ClientSignedChannelSetup={2}&clientCommitedAmount={3}&hubCommitedAmount={4}&clientPubkey={5}&hubPrivatekey={6}&assetName={7}&counterPartyRevokePubkey={8}&activationIn10Minutes={9}",
                    Settings.WalletBackendUrl, resp.UnsigndTransaction, clientSignedTx, 30, 75, clientPrivateKey.PubKey,
                     hubPrivateKey.ToString(), "TestExchangeUSD", hubSelfRevokKey.PubKey, 144);
                response = await client.GetStringAsync(url);
                var signedResp = JsonConvert.DeserializeObject<UnsignedClientCommitmentTransactionResponse>(response);

                var signedSetup = new Transaction(signedResp.FullySignedSetupTransaction);
                var rpcClient = GetRPCClient(Settings);
                await rpcClient.SendRawTransactionAsync(signedSetup);
                await GenerateBlocks(Settings, 1);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                    new string[] { signedSetup.GetHash().ToString() }, null);

                return signedResp;
            }
        }

        [Test]
        public async Task SetupTransactionAndClientCommitmentSendingSuccessful()
        {
            try
            {
                var privateKeys = reservedPrivateKey;

                var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
                var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
                var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
                var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

                var signedResp = await GetOffchainSignedSetup(privateKeys);

                var clientSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = signedResp.UnsignedClientCommitment0,
                    PrivateKey = clientPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                var hubSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = clientSignedCommitment,
                    PrivateKey = hubPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                await AddEnoughPaymentFeeAndBroadcast(Settings.WalletBackendUrl, hubSignedCommitment);


            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        [Test]
        public async Task CommitmentComplexOutputSpendingBeforeBecomingFinal()
        {
            try
            {
                var privateKeys = CreatePrivateKeys();

                var spendingToBroadcast = new Transaction(await GetCommitmentComplexOutputSpendingForBIP68Output(privateKeys));

                // Generating not enough blocks
                var blocks = await GenerateBlocks(Settings, 10);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasBlockIndexed, blocks);

                var rpcClient = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential
                            (WebSettings.ConnectionParams.Username, WebSettings.ConnectionParams.Password),
                                    WebSettings.ConnectionParams.IpAddress, WebSettings.ConnectionParams.BitcoinNetwork);
                await rpcClient.SendRawTransactionAsync(spendingToBroadcast);
                await GenerateBlocks(Settings, 142);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                    new string[] { spendingToBroadcast.GetHash().ToString() }, null);
            }
            catch (Exception exp)
            {
                Assert.IsTrue(exp.ToString().Contains("non-BIP68-final"));
            }
        }

        [Test]
        public async Task CommitmentComplexOutputSpendingAfterBecomingFinal()
        {
            try
            {
                var privateKeys = CreatePrivateKeys();
                var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
                var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
                var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
                var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

                var spendingToBroadcast = await GetCommitmentComplexOutputSpendingForBIP68Output(privateKeys);
                
                // Generating enough blocks
                var blocks = await GenerateBlocks(Settings, 144);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasBlockIndexed, blocks);

                // Sending the spending tx
                var rpcClient = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential
                            (WebSettings.ConnectionParams.Username, WebSettings.ConnectionParams.Password),
                                    WebSettings.ConnectionParams.IpAddress, WebSettings.ConnectionParams.BitcoinNetwork);
                await rpcClient.SendRawTransactionAsync(new Transaction(spendingToBroadcast));


                // Hub should have the correct value
                var currentBalance = await GetAssetBalanceOfAddress(hubPrivateKey.GetAddress().ToString(), "TestExchangeUSD");
                Assert.AreEqual(90 + 75, currentBalance);
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        private async Task<string> GetCommitmentComplexOutputSpendingForBIP68Output(string[] privateKeys)
        {
            try
            {
                var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
                var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
                var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
                var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

                var signedResp = await GetOffchainSignedSetup(privateKeys);

                var clientSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = signedResp.UnsignedClientCommitment0,
                    PrivateKey = clientPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                var hubSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = clientSignedCommitment,
                    PrivateKey = hubPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                var txSendingResult = await AddEnoughPaymentFeeAndBroadcast(Settings.WalletBackendUrl, hubSignedCommitment);

                using (HttpClient client = new HttpClient())
                {
                    var url = string.Format("{0}/Offchain/CreateCommitmentSpendingTransactionForTimeActivatePart?commitmentTransactionHex={1}&spendingPrivateKey={2}&clientPubkey={3}&hubPubkey={4}&assetName={5}&lockingPubkey={6}&activationIn10Minutes={7}&clientSendsCommitmentToHub={8}",
                        Settings.WalletBackendUrl, txSendingResult, hubPrivateKey, clientPrivateKey.PubKey, hubPrivateKey.PubKey, "TestExchangeUSD", hubSelfRevokKey.PubKey, 144, true);
                    var response = await client.GetStringAsync(url);
                    var commitmentSpendingResp = JsonConvert.DeserializeObject<CreateCommitmentSpendingTransactionForTimeActivatePartResponse>
                        (response);

                    return commitmentSpendingResp.TransactionHex;
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        private string GetUrlForCommitmentCreation(string baseUrl, string signedSetupTransaction, string clientPubkey,
            string hubPubkey, double clientAmount, double hubAmount, string assetName, string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub)
        {

            return string.Format("{0}/Offchain/CreateUnsignedCommitmentTransactions?signedSetupTransaction={1}&clientPubkey={2}&hubPubkey={3}&clientAmount={4}&hubAmount={5}&assetName={6}&lockingPubkey={7}&activationIn10Minutes={8}&clientSendsCommitmentToHub={9}",
                baseUrl, signedSetupTransaction, clientPubkey, hubPubkey, clientAmount, hubAmount, assetName,
                lockingPubkey, activationIn10Minutes, clientSendsCommitmentToHub);
        }

        private async static Task<string> AddEnoughPaymentFeeAndBroadcast(string baseUrl, string txTosend)
        {
            using (HttpClient client = new HttpClient())
            {
                var url = string.Format("{0}/Offchain/AddEnoughFeesToCommitentAndBroadcast?commitmentTransaction={1}",
                    baseUrl, txTosend);
                var response = await client.GetStringAsync(url);
                AddEnoughFeesToCommitentAndBroadcastResponse commitmentBroadcastResponse =
                    JsonConvert.DeserializeObject<AddEnoughFeesToCommitentAndBroadcastResponse>(response);
                var blocks = await GenerateBlocks(Settings, 1);
                await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                    new string[] { commitmentBroadcastResponse.TransactionId }, null);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasBlockIndexed, blocks);

                return commitmentBroadcastResponse.TransactionHex;
            }
        }

        [Test]
        public async Task CreateBroadcastClientCommitmentYeldsCorrectClientBalance()
        {
            try
            {
                var privateKeys = CreatePrivateKeys();

                var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
                var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
                var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
                var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

                var signedResp = await GetOffchainSignedSetup(privateKeys);
                var url = GetUrlForCommitmentCreation(Settings.WalletBackendUrl, signedResp.FullySignedSetupTransaction, clientPrivateKey.PubKey.ToHex(),
                    hubPrivateKey.PubKey.ToHex(), 40, 65, "TestExchangeUSD", clientPrivateKey.PubKey.ToHex(), 10, true);

                string response;
                CreateUnsignedCommitmentTransactionsResponse unsignedCommitment;

                using (HttpClient client = new HttpClient())
                {
                    response = await client.GetStringAsync(url);
                    unsignedCommitment =
                        JsonConvert.DeserializeObject<CreateUnsignedCommitmentTransactionsResponse>(response);
                }

                var clientSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = unsignedCommitment.UnsignedCommitment,
                    PrivateKey = clientPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                var hubSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = clientSignedCommitment,
                    PrivateKey = hubPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                await AddEnoughPaymentFeeAndBroadcast(Settings.WalletBackendUrl, hubSignedCommitment);

                var currentBalance = await GetAssetBalanceOfAddress(clientPrivateKey.GetAddress().ToString(), "TestExchangeUSD");
                Assert.AreEqual(90 + 40, currentBalance);

                currentBalance = await GetAssetBalanceOfAddress(hubPrivateKey.GetAddress().ToString(), "TestExchangeUSD");
                Assert.AreEqual(90, currentBalance);
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        [Test]
        public async Task CreateBroadcastHubCommitmentYeldsCorrectHubBalance()
        {
            try
            {
                var privateKeys = CreatePrivateKeys();

                var clientPrivateKey = new BitcoinSecret(privateKeys[0]);
                var hubPrivateKey = new BitcoinSecret(privateKeys[1]);
                var clientSelfRevokeKey = new BitcoinSecret(privateKeys[2]);
                var hubSelfRevokKey = new BitcoinSecret(privateKeys[3]);

                var signedResp = await GetOffchainSignedSetup(privateKeys);
                var url = GetUrlForCommitmentCreation(Settings.WalletBackendUrl, signedResp.FullySignedSetupTransaction, clientPrivateKey.PubKey.ToHex(),
                    hubPrivateKey.PubKey.ToHex(), 40, 65, "TestExchangeUSD", clientPrivateKey.PubKey.ToHex(), 10, false);

                string response;
                CreateUnsignedCommitmentTransactionsResponse unsignedCommitment;

                using (HttpClient client = new HttpClient())
                {
                    response = await client.GetStringAsync(url);
                    unsignedCommitment =
                        JsonConvert.DeserializeObject<CreateUnsignedCommitmentTransactionsResponse>(response);
                }

                var clientSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = unsignedCommitment.UnsignedCommitment,
                    PrivateKey = clientPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                var hubSignedCommitment = await SignTransactionWorker(new TransactionSignRequest
                {
                    TransactionToSign = clientSignedCommitment,
                    PrivateKey = hubPrivateKey.ToString()
                }, SigHash.All | SigHash.AnyoneCanPay);

                await AddEnoughPaymentFeeAndBroadcast(Settings.WalletBackendUrl, hubSignedCommitment);

                var currentBalance = await GetAssetBalanceOfAddress(hubPrivateKey.GetAddress().ToString(), "TestExchangeUSD");
                Assert.AreEqual(90 + 65, currentBalance);

                currentBalance = await GetAssetBalanceOfAddress(clientPrivateKey.GetAddress().ToString(), "TestExchangeUSD");
                Assert.AreEqual(90, currentBalance);
            }
            catch (Exception exp)
            {
                throw exp;
            }
        }

        public static string[] CreatePrivateKeys()
        {
            string[] privateKeys = new string[4];

            for (int i = 0; i < privateKeys.Count(); i++)
            {
                while (true)
                {
                    privateKeys[i] = (new Key()).GetWif(WebSettings.ConnectionParams.BitcoinNetwork).ToWif();
                    if (!reservedPrivateKey.Contains(privateKeys[i]))
                    {
                        break;
                    }
                }
            }
            return privateKeys;
        }

        private async Task<double> GetAssetBalanceOfAddress(string address, string assetName)
        {
            GetCurrentBalanceModel getCurrentBalanceModel = new GetCurrentBalanceModel
            {
                TransactionId = "10",
                MinimumConfirmation = 0,
                MultisigAddress = address

            };
            var reply = await CreateLykkeWalletRequestAndProcessResult<GetCurrentBalanceResponse>
                ("GetCurrentBalance", getCurrentBalanceModel, QueueReader, QueueWriter);
            if (reply.Error != null)
            {
                throw new Exception(reply.Error.Message);
            }
            else
            {
                foreach (var item in reply.Result.ResultArray)
                {
                    if (item.Asset.ToLower() == assetName.ToLower())
                    {
                        return item.Amount;
                    }
                }
            }

            throw new Exception(string.Format("Something went wrong getting balance for address {0}, for asset {1}.",
                address, assetName));
        }

        private static async Task CashinToAddress(string destAddress, string asset, double amount)
        {
            CashInRequestModel cashin = new CashInRequestModel
            {
                TransactionId = "10",
                MultisigAddress = destAddress,
                Amount = amount,
                Currency = asset
            };
            var reply = await CreateLykkeWalletRequestAndProcessResult<CashInResponse>
                ("CashIn", cashin, QueueReader, QueueWriter);
            await GenerateBlocks(Settings, 1);
            await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                new string[] { reply.Result.TransactionHash }, null);
            await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasBalanceIndexed,
                new string[] { reply.Result.TransactionHash }, destAddress);
        }
    }
}
