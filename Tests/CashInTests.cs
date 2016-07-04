using NUnit.Framework;
using System.Threading.Tasks;

namespace Lykkex.WalletBackend.Tests
{
    [TestFixture]
    public class CashInTests : TaskTestsCommon
    {
        [Test]
        public async Task DesiredAmountCashedIn()
        {
            var multisig = (await GenerateNewWalletTests.GenerateNewWallet(QueueReader, QueueWriter)).MultiSigAddress;
            var amount = 5000;
            var asset = "TestExchangeUSD";
            CashInRequestModel cashin = new CashInRequestModel { TransactionId = "10", MultisigAddress = multisig, Amount = amount , Currency = asset };
            var reply = await CreateLykkeWalletRequestAndProcessResult<CashInResponse>
                ("CashIn", cashin, QueueReader, QueueWriter);
            await GenerateBlocks(Settings, 1);
            await WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                new string[] { reply.Result.TransactionHash }, null);
            await WaitUntillQBitNinjaHasIndexed(Settings, HasBalanceIndexed,
                new string[] { reply.Result.TransactionHash }, multisig);

            var balance = await GetCurrentBalanceTests.GetAssetBalanceForMultisig(multisig, asset, 0, QueueReader, QueueWriter);

            Assert.AreEqual(amount, balance, "The balance sent is not correct.");
        }
    }

    public class CashInRequestModel : BaseRequestModel
    {
        public string MultisigAddress
        {
            get;
            set;
        }

        public float Amount
        {
            get;
            set;
        }

        public string Currency
        {
            get;
            set;
        }
    }
}
