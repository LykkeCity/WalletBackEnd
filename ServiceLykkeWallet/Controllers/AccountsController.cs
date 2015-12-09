using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using LykkeWalletServices.Accounts;
using ServiceLykkeWallet.Models;
using Core;
using NBitcoin;
using System;

namespace ServiceLykkeWallet.Controllers
{
    public class AccountsController : ApiController
    {
        // curl -X POST http://localhost:8085/Accounts/GenerateAddresses -H "Content-Type:application/json" -d "=Main"
        // The story of = sign is stated here: http://encosia.com/using-jquery-to-post-frombody-parameters-to-web-api/
        [HttpPost]
        public IHttpActionResult GenerateAddresses([FromBody]NetworkType network)
        {
            var srvAccountGenerator = new SrvAccountGenerator();
            var result = srvAccountGenerator.GenerateAccount(network);
            return Json(ConvertAccountModelToAccountContract(result));
        }

        private GenerateAccountContract ConvertAccountModelToAccountContract(AccountModel m)
        {
            GenerateAccountContract contract = new GenerateAccountContract();
            contract.Network = m.NetworkType.ToString();
            contract.PrivateKey = m.Key.ToString(GetNetwork(m.NetworkType));
            contract.WalletAddress = m.Key.PubKey.ToString(GetNetwork(m.NetworkType));
            return contract;
        }

        /// <summary>
        /// Returns the NBitcoin Network object for the accout network type
        /// </summary>
        /// <returns></returns>
        private Network GetNetwork(NetworkType network)
        {
            switch (network)
            {
                case NetworkType.Main:
                    return NBitcoin.Network.Main;
                case NetworkType.TestNet:
                    return NBitcoin.Network.TestNet;
                default:
                    // we will never reach here
                    return null;
            }
        }


        [HttpGet]
        public IHttpActionResult GetBalance(string publicAddress)
        {
            var srvAccountBalanceAccess = new SrvAccountBalanceAccess();
            return Json(srvAccountBalanceAccess.GetAccountBalances(publicAddress)
                .Select(itm => new
                {
                    amount = itm.Amount,
                    asset = itm.Asset
                }
                ));
        }
    }
}
