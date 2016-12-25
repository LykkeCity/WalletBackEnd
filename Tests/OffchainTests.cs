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
        [Test]
        public async Task SetupTransactionAndClientCommitmentSendingSuccessful()
        {
            try
            {
                var clientPrivateKey = new BitcoinSecret("cQMqC1Vqyi6o62wE1Z1ZeWDbMCkRDZW5dMPJz8QT9uMKQaMZa8JY");
                var hubPrivateKey = new BitcoinSecret("cQyt2zxAS2uV7HJWR9hf16pFDTye8YsGL6hzd9pQzMoo9m24RGoV");
                var clientSelfRevokeKey = new BitcoinSecret("cSFbgd8zKDSCDHgGocccngyVSfGZsyZFiTXtimTonHyL44gTKTNU");
                var hubSelfRevokKey = new BitcoinSecret("cPBtsvLrD3DnbdGgDZ2EMbZnQurzBVmgmejiMv55jH9JehPDn5Aq");

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

                    url = string.Format("{0}/Offchain/AddEnoughFeesToCommitentAndBroadcast?commitmentTransaction={1}",
                        Settings.WalletBackendUrl, hubSignedCommitment);
                    response = await client.GetStringAsync(url);
                    AddEnoughFeesToCommitentAndBroadcastResponse commitmentBroadcastResponse =
                        JsonConvert.DeserializeObject<AddEnoughFeesToCommitentAndBroadcastResponse>(response);
                    await GenerateBlocks(Settings, 1);
                    await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                        new string[] { commitmentBroadcastResponse.TransactionId }, null);
                }
            }
            catch (Exception exp)
            {
                throw exp;
            }
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
