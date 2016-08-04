using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using NBitcoin;
using ServiceLykkeWallet.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace ServiceLykkeWallet.Controllers
{
    public class GeneralController : ApiController
    {
        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetVersion()
        {
            // With the help of http://stackoverflow.com/questions/909555/how-can-i-get-the-assembly-file-version

            Version version = Assembly.GetEntryAssembly().GetName().Version;

            var result = new HttpResponseMessage(HttpStatusCode.Accepted);
            result.Content = new StringContent(version.ToString());

            return result;
        }

        [System.Web.Http.HttpGet]
        public async Task<HttpResponseMessage> FeeRate()
        {
            var feeRate = (await OpenAssetsHelper.GetFeeRate()).FeePerK.Satoshi.ToString();

            var result = new HttpResponseMessage(HttpStatusCode.Accepted);
            result.Content = new StringContent(feeRate);

            return result;
        }

        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetWallets()
        {
            StringBuilder builder = new StringBuilder();

            foreach(var item in OpenAssetsHelper.P2PKHDictionary)
            {
                builder.Append("Wallet: ");
                builder.Append(item.Key);
                builder.Append(" Pubkey: ");
                builder.Append((new BitcoinSecret(item.Value)).PubKey.ToString());
                builder.AppendLine();
            }

            var result = new HttpResponseMessage(HttpStatusCode.Accepted);
            result.Content = new StringContent(builder.ToString());

            return result;
        }
    }
}
