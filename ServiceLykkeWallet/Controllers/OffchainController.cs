using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceLykkeWallet.Controllers
{
    public class OffchainController: ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> AddClientProvidedChannelInput(string multiSig, string transactionId)
        {
            return Ok();
        }

        public async Task<IHttpActionResult> GenerateOnChainTx0(string clientMultiSig, string clientContributedTransactionHex,
            string clientContributedAmount, string ChannelAssetName)
        {
            return Ok();
        }

        public async Task<IHttpActionResult> FinalizeChannelSetup(string channelId, string clientCommitment0)
        {
            return Ok();
        }

    }
}
