using LykkeWalletServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;

namespace NonBitcoinTests
{
    [TestFixture]
    public class OpenAssetsHelperTest
    {
        [Test]
        public async Task IsInputStillSpendableSuccess()
        {
            var txId = uint256.Parse("51fb5c93e4a8fb8189154e8af6492f46caf22d78be070ede4bbcad5569e74a1b");
            var spendableTxIn = new TxIn(new OutPoint(txId, 0));
            bool spendable = await OpenAssetsHelper.IsInputStillSpendable(spendableTxIn, SettingsReader.Settings.RegtestRPCUsername,
                SettingsReader.Settings.RegtestRPCPassword, SettingsReader.Settings.RegtestRPCIP, SettingsReader.Settings.Network);
            Assert.IsTrue(spendable, "A spendable output is reported as unspendable.");
        }

        [Test]
        public async Task IsInputStillSpendableFailure()
        {
            var txId = uint256.Parse("51fb5c93e4a8fb8189154e8af6492f46caf22d78be070ede4bbcad5569e74a1b");
            var nonSpendableTxIn = new TxIn(new OutPoint(txId, 2));
            bool spendable = await OpenAssetsHelper.IsInputStillSpendable(nonSpendableTxIn, SettingsReader.Settings.RegtestRPCUsername,
                SettingsReader.Settings.RegtestRPCPassword, SettingsReader.Settings.RegtestRPCIP, SettingsReader.Settings.Network);
            Assert.IsFalse(spendable, "A unspendable output is reported as spendable.");
        }

        [Test]
        public async Task GetNumberOfTransactionConfirmationSuccess()
        {
            var txId = "84230f5e12060ef7e8947354c3fc9e8d616f3d8d56c77923c5ad2e923ce60f13";

            OpenAssetsHelper.QBitNinjaBaseUrl = SettingsReader.Settings.QBitNinjaBaseUrl;

            int numOfConfirmations = await 
                OpenAssetsHelper.GetNumberOfTransactionConfirmations(txId);

            Assert.Greater(numOfConfirmations, 267000, string.Format(
                "Number of confirmations for transaction {0} should be greater than 267000.", txId));
        }
    }
}
