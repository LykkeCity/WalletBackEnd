using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceLykkeWallet.Controllers
{
    public class OffchainController : ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> AddClientProvidedChannelInput(string multiSig, string transactionId)
        {
            return Ok();
        }

        public class UnsignedChannelSetupTransaction
        {
            public string UnsigndTransaction
            {
                get;
                set;
            }
        }

        // http://localhost:8989/Offchain/GenerateUnsignedChannelSetupTransaction?ClientAddress=x&ClientContributedAmount=10&HubAddress=z&HubContributedAmount=10&ClientMultisigAddress=ab&ClientMultisigContributedAmount=10&ChannelAssetName=ac
        [HttpGet]
        public async Task<IHttpActionResult> GenerateUnsignedChannelSetupTransaction(string ClientAddress, double ClientContributedAmount,
            string HubAddress, double HubContributedAmount, string ClientMultisigAddress, double ClientMultisigContributedAmount, string ChannelAssetName)
        {
            return Json(new UnsignedChannelSetupTransaction { UnsigndTransaction = "0001" });
        }

        public class UnsignedClientCommitmentTransactionResponse
        {
            public string FullySignedSetupTransaction
            {
                get;
                set;
            }

            public string UnsignedClientCommitment0
            {
                get;
                set;
            }
        }

        // http://localhost:8989/Offchain/CreateUnsignedClientCommitmentTransaction?UnsignedChannelSetupTransaction=0001&ClientSignedChannelSetup=0001
        [HttpGet]
        public async Task<IHttpActionResult> CreateUnsignedClientCommitmentTransaction(string UnsignedChannelSetupTransaction, string ClientSignedChannelSetup)
        {
            return Json(new UnsignedClientCommitmentTransactionResponse { FullySignedSetupTransaction = "0002",
                UnsignedClientCommitment0 = "0002" });
        }

        public class FinalizeChannelSetupResponse
        {
            public string SignedHubCommitment0
            {
                get;
                set;
            }
        }

        // http://localhost:8989/Offchain/FinalizeChannelSetup?FullySignedSetupTransaction=002&SignedClientCommitment0=002
        [HttpGet]
        public async Task<IHttpActionResult> FinalizeChannelSetup(string FullySignedSetupTransaction, string SignedClientCommitment0)
        {
            return Json(new FinalizeChannelSetupResponse { SignedHubCommitment0 = "0003" });
        }
    }
}
