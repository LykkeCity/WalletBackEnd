using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using NBitcoin;
using ServiceLykkeWallet.Models;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Mvc;

namespace ServiceLykkeWallet.Controllers
{
    public class PrivateKeyController : ApiController
    {
        // This should respond to curl -X POST http://localhost:8989/PrivateKey/Add -d "=cQKNnKS7TUFPdVc4muGXq8X9h5dxuGyYBSbnFUUuv9NVsLDNFP51"
        [System.Web.Http.HttpPost]
        public ActionResult Add([FromBody]string privatekey)
        {
            try
            {
                BitcoinSecret secret = BitcoinSecret.GetFromBase58Data(privatekey) as BitcoinSecret;
                OpenAssetsHelper.AddPrivateKey(privatekey);
                return new HttpStatusCodeResult((int)HttpStatusCode.OK);

            }
            catch (Exception)
            {
                return new HttpStatusCodeResult((int)HttpStatusCode.BadRequest);
            }
        }
    }
}
