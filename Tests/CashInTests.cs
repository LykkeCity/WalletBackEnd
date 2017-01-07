using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;

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
            CashInRequestModel cashin = new CashInRequestModel { TransactionId = Guid.NewGuid().ToString(),
                MultisigAddress = multisig, Amount = amount , Currency = asset };
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

    public class SwapRequestModel : BaseRequestModel
    {
        public string MultisigCustomer1 { get; set; }
        public double Amount1 { get; set; }
        public string Asset1 { get; set; }
        public string MultisigCustomer2 { get; set; }
        public double Amount2 { get; set; }
        public string Asset2 { get; set; }
    }

    public class CashInRequestModel : BaseRequestModel
    {
        public string MultisigAddress
        {
            get;
            set;
        }

        public double Amount
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
