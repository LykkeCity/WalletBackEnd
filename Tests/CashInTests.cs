using NBitcoin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;
using Lykkex.WalletBackend.Tests.GetCurrentBalance;
using Lykkex.WalletBackend.Tests.GenerateNewWallet;

namespace Lykkex.WalletBackend.Tests.CashIn
{
    [TestFixture]
    public class CashInTests : TaskTestsCommon
    {
        [Test]
        public void DesiredAmountCashedIn()
        {
            var multisig = GenerateNewWalletTests.GenerateNewWallet(QueueReader, QueueWriter).MultiSigAddress;
            var amount = 5000;
            var asset = "TestExchangeUSD";
            CashInRequestModel cashin = new CashInRequestModel { TransactionId = "10", MultisigAddress = multisig, Amount = amount , Currency = asset };
            var reply = CreateLykkeWalletRequestAndProcessResult<CashInResponse>("CashIn", cashin, QueueReader, QueueWriter);

            var balance = GetCurrentBalanceTests.GetAssetBalanceForMultisig(multisig, asset, 0, QueueReader, QueueWriter);

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
