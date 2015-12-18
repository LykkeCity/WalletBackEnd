using Core;
using LykkeWalletServices.Accounts;
using NBitcoin;
using ServiceLykkeWallet.Models;
using System.Linq;
using System.Web.Http;
using static ServiceLykkeWallet.SettingsReader;

namespace ServiceLykkeWallet.Controllers
{
    public class AccountsController : ApiController
    {
        // curl -X POST http://localhost:8085/Accounts/GenerateAddresses -d "=Main"
        // The story of = sign is stated here: http://encosia.com/using-jquery-to-post-frombody-parameters-to-web-api/
        [HttpPost]
        public IHttpActionResult GenerateAddresses([FromBody]NetworkType network)
        {
            var srvAccountGenerator = new SrvAccountGenerator();
            var result = srvAccountGenerator.GenerateAccount(network);
            return Json(ConvertAccountModelToAccountContract(result));
        }

        /// <summary>
        /// Returns assets type supported by the exchange
        /// </summary>
        /// <returns>An array of available assets in json format</returns>
        /// <remarks>
        /// Configed value are in settings.json
        /// Sample url: http://localhost:8085/Accounts/GetAvailableAssets
        /// Sample response: [{"AssetId":"ARe5TkHAjAZubkBMCBomNn93m9ZV6HGFqg","Name":"bjkUSD"},{"AssetId":"ASYfetm7ue3Pk5NyK9NDdGU9mWHApaPuur","Name":"bjkEUR"}]
        /// </remarks>
        [HttpGet]
        public IHttpActionResult GetAvailableAssets()
        {
            return Json((AssetDefinition[])Configuration.Properties["assets"]);
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
