using NBitcoin;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lykkex.WalletBackend.Tests
{
    [TestFixture]
    public class GenerateNewWalletTests : TaskTestsCommon
    {
        [Test]
        public async Task GenerateNewWalletSuccessfully()
        {
            var newWallet = await GenerateNewWallet(QueueReader, QueueWriter);
            Assert.IsNotNull(newWallet.ColoredMultiSigAddress);
            Assert.IsNotNull(newWallet.ColoredWalletAddress);
            Assert.IsNotNull(newWallet.MultiSigAddress);
            Assert.IsNotNull(newWallet.WalletAddress);
            Assert.IsNotNull(newWallet.WalletPrivateKey);

            Assert.IsNotEmpty(newWallet.ColoredMultiSigAddress);
            Assert.IsNotEmpty(newWallet.ColoredWalletAddress);
            Assert.IsNotEmpty(newWallet.MultiSigAddress);
            Assert.IsNotEmpty(newWallet.WalletAddress);
            Assert.IsNotEmpty(newWallet.WalletPrivateKey);
        }

        [Test]
        public async Task GenerateNewWalletValidWallet()
        {
            var newWallet = await GenerateNewWallet(QueueReader, QueueWriter);

            Assert.AreEqual(newWallet.WalletAddress, (new BitcoinSecret(newWallet.WalletPrivateKey, Settings.Network).GetAddress() as BitcoinAddress).ToWif(),
                "Bitcoin addresses are the same.");

            var calculatedMultiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { (new BitcoinSecret(newWallet.WalletPrivateKey)).PubKey,
                (new BitcoinSecret(Settings.ExchangePrivateKey)).PubKey }).GetScriptAddress(Settings.Network).ToWif();
            Assert.AreEqual(calculatedMultiSigAddress, newWallet.MultiSigAddress,
                "The expected multisig does not match the returned one.");

            Assert.AreEqual((BitcoinAddress.GetFromBase58Data(newWallet.WalletAddress) as BitcoinAddress).ToColoredAddress().ToWif(),
                newWallet.ColoredWalletAddress, "Colored addres for private wallet is invalid.");
            Assert.AreEqual((BitcoinAddress.GetFromBase58Data(newWallet.MultiSigAddress) as BitcoinAddress).ToColoredAddress().ToWif(),
                newWallet.ColoredMultiSigAddress, "Colored addres for multisig wallet is invalid.");
        }
        public async static Task<Core.GenerateNewWalletTaskResult> GenerateNewWallet
            (AzureStorage.AzureQueueExt QueueReader, AzureStorage.AzureQueueExt QueueWriter)
        {
            GenerateNewWalletModel generateNewWallet = new GenerateNewWalletModel { TransactionId = "10" };

            var reply = await CreateLykkeWalletRequestAndProcessResult<GenerateNewWalletResponse>("GenerateNewWallet", generateNewWallet,
                QueueReader, QueueWriter);

            return reply.Result;
        }
    }

    public class GenerateNewWalletModel : BaseRequestModel
    {
    }
}
