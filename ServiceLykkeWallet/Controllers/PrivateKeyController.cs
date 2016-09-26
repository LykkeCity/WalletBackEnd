using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using NBitcoin;
using Newtonsoft.Json;
using ServiceLykkeWallet.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using System.Text.RegularExpressions;

namespace ServiceLykkeWallet.Controllers
{
    public class PrivateKeyController : ApiController
    {
        // This should respond to curl -H "Content-Type: application/json" -X POST -d "{\"PrivateKey\":\"xyz\",\"IsP2PKH\":\"true\"}" http://localhost:8989/PrivateKey/Add
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> Add(AddPrivateKeyContract privatekey)
        {
            IHttpActionResult result = null;

            try
            {
                OpenAssetsHelper.AddPrivateKey(privatekey.PrivateKey, privatekey.IsP2PKH);
                result = Ok();

            }
            catch (Exception ex)
            {
                result = InternalServerError(ex);
            }

            var pvPart01 = privatekey.PrivateKey.Substring(0, 5);
            var pvPart02 = privatekey.PrivateKey.Substring(5);
            Regex regex = new Regex("[a-zA-Z0-9]");
            pvPart02 = regex.Replace(pvPart02, "x");
            var pv = new AddPrivateKeyContract { IsP2PKH = privatekey.IsP2PKH, PrivateKey = pvPart01 + pvPart02 };
            await OpenAssetsHelper.SendPendingEmailsAndLogInputOutput
                (WebSettings.ConnectionString, "PrivateKeyAdd:" + JsonConvert.SerializeObject(pv),
                TransactionsController.ConvertResultToString(result));

            return result;
        }
    }
}
