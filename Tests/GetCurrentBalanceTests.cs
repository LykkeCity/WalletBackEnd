using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

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
    }
}
