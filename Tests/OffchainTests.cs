using LykkeWalletServices;
using LykkeWalletServices.Transactions.Responses;
using Lykkex.WalletBackend.Tests;
using NBitcoin;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            "cSFbgd8zKDSCDHgGocccngyVSfGZsyZFiTXtimTonHyL44gTKTNU",
            "cPBtsvLrD3DnbdGgDZ2EMbZnQurzBVmgmejiMv55jH9JehPDn5Aq"
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

                url = string.Format("{0}/Offchain/CreateUnsignedClientCommitmentTransaction?UnsignedChannelSetupTransaction={1}&ClientSignedChannelSetup={2}&clientCommitedAmount={3}&hubCommitedAmount={4}&clientPubkey={5}&hubPrivatekey={6}&assetName={7}&selfRevokePubkey={8}&activationIn10Minutes={9}",
                    Settings.WalletBackendUrl, resp.UnsigndTransaction, clientSignedTx, 30, 75, clientPrivateKey.PubKey, hubPrivateKey.ToString(), "TestExchangeUSD", clientSelfRevokeKey.PubKey, 144);
                response = await client.GetStringAsync(url);
                var signedResp = JsonConvert.DeserializeObject<UnsignedClientCommitmentTransactionResponse>(response);

                var signedSetup = new Transaction(signedResp.FullySignedSetupTransaction);
                var rpcClient = GetRPCClient(Settings);
                await rpcClient.SendRawTransactionAsync(signedSetup);
                await GenerateBlocks(Settings, 1);
                await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
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

        private string GetUrlForCommitmentCreation(string baseUrl, string signedSetupTransaction, string clientPubkey,
            string hubPubkey, double clientAmount, double hubAmount, string assetName, string lockingPubkey, int activationIn10Minutes, bool clientSendsCommitmentToHub)
        {
            return string.Format("{0}/Offchain/CreateUnsignedCommitmentTransactions?signedSetupTransaction={1}&clientPubkey={2}&hubPubkey={3}&clientAmount={4}&hubAmount={5}&assetName={6}&lockingPubkey={7}&activationIn10Minutes={8}&clientSendsCommitmentToHub={9}",
                baseUrl, signedSetupTransaction, clientPubkey, hubPubkey, clientAmount, hubAmount, assetName,
                lockingPubkey, activationIn10Minutes, clientSendsCommitmentToHub);
        }

        private async static Task AddEnoughPaymentFeeAndBroadcast(string baseUrl, string txTosend)
        {
            using (HttpClient client = new HttpClient())
            {
                var url = string.Format("{0}/Offchain/AddEnoughFeesToCommitentAndBroadcast?commitmentTransaction={1}",
                    baseUrl, txTosend);
                var response = await client.GetStringAsync(url);
                AddEnoughFeesToCommitentAndBroadcastResponse commitmentBroadcastResponse =
                    JsonConvert.DeserializeObject<AddEnoughFeesToCommitentAndBroadcastResponse>(response);
                await GenerateBlocks(Settings, 1);
                await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                    new string[] { commitmentBroadcastResponse.TransactionId }, null);
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
