using NBitcoin;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft;

namespace Lykkex.WalletBackend.Tests.GenerateNewWallet
{
    public class GenerateNewWalletModel : BaseRequestModel
    {
    }

    [TestFixture]
    public class GenerateNewWalletTests : TaskTestsCommon
    {
        public static Core.GenerateNewWalletTaskResult GenerateNewWallet(AzureStorage.AzureQueueExt QueueReader, AzureStorage.AzureQueueExt QueueWriter)
        {
            GenerateNewWalletModel generateNewWallet = new GenerateNewWalletModel { TransactionId = "10" };

            var reply = CreateLykkeWalletRequestAndProcessResult<GenerateNewWalletResponse>("GenerateNewWallet", generateNewWallet,
                QueueReader, QueueWriter);

            return reply.Result;
        }
    }
}
