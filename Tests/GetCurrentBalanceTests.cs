using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;

namespace Lykkex.WalletBackend.Tests
{
    public class GetCurrentBalanceModel : BaseRequestModel
    {
        public string MultisigAddress
        {
            get;
            set;
        }

        public int MinimumConfirmation
        {
            get;
            set;
        }
    }

    [TestFixture]
    public class GetCurrentBalanceTests : TaskTestsCommon
    {
        public async static Task<float> GetAssetBalanceForMultisig(string multisig, string assetName, int confirmationNamber,
            AzureStorage.AzureQueueExt QueueReader, AzureStorage.AzureQueueExt QueueWriter)
        {
            GetCurrentBalanceModel getCurrentBalance = new GetCurrentBalanceModel { TransactionId = "10", MultisigAddress = multisig, MinimumConfirmation = confirmationNamber };

            var reply = await CreateLykkeWalletRequestAndProcessResult<GetCurrentBalanceResponse>("GetCurrentBalance", getCurrentBalance,
                QueueReader, QueueWriter);

            return reply.Result.ResultArray.Where(item => item.Asset == assetName).FirstOrDefault()?.Amount ?? -1;
        }

        [Test]
        public async Task TestWhenContinuationOfQBitNinjaGetsActive()
        {
            var multisig = Base58Data.GetFromBase58Data("2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo")
                as BitcoinAddress;
            string[] txId = new string[500];
            for (int i = 0; i < txId.Count(); i++)
            {
                txId[i] = await SendBTC(Settings, multisig, 0.0001f);
            }

            var dummyTxId = await SendBTC(Settings, MassBitcoinHolder, 0.0001f);

            await GenerateBlocks(Settings, 1);
            await WaitUntillQBitNinjaHasIndexed(Settings, HasBalanceIndexed,
                new string[] { dummyTxId }, multisig.ToWif());
        }
    }
}
