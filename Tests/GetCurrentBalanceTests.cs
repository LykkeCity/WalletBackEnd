using NBitcoin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;

namespace Lykkex.WalletBackend.Tests.GetCurrentBalance
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
        public static float GetAssetBalanceForMultisig(string multisig, string assetName, int confirmationNamber,
            AzureStorage.AzureQueueExt QueueReader, AzureStorage.AzureQueueExt QueueWriter)
        {
            GetCurrentBalanceModel getCurrentBalance = new GetCurrentBalanceModel { TransactionId = "10", MultisigAddress = multisig, MinimumConfirmation = confirmationNamber };

            var reply = CreateLykkeWalletRequestAndProcessResult<GetCurrentBalanceResponse>("GetCurrentBalance", getCurrentBalance,
                QueueReader, QueueWriter);

            return reply.Result.ResultArray.Where(item => item.Asset == assetName).FirstOrDefault()?.Amount ?? -1;
        }
    }
}
