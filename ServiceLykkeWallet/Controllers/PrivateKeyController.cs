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
        // This should respond to curl -H "Content-Type: application/json" -X POST -d "{\"PrivateKey\":\"xyz\",\"IsP2PKH\":\"true\"}" http://localhost:8989/PrivateKey/Add
        [System.Web.Http.HttpPost]
        public IHttpActionResult Add(AddPrivateKeyContract privatekey)
        {
            try
            {
                OpenAssetsHelper.AddPrivateKey(privatekey.PrivateKey, privatekey.IsP2PKH);
                return Ok();

            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
