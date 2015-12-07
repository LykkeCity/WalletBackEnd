using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using LykkeWalletServices.Accounts;
using ServiceLykkeWallet.Models;

namespace ServiceLykkeWallet.Controllers
{
    public class AccountsController : ApiController
    {

        [HttpPost]
        public IHttpActionResult GenerateAddresses()
        {
            var srvAccountGenerator = new SrvAccountGenerator();
            var result = srvAccountGenerator.GenerateAccount();
            return Json(
                new GenerateAccountContract
                {
                    PrivateKey = result.PrivateKey,
                    MultisigPublicAddress = result.MultisigPublicAddress,
                    CcPublicAddress = result.CcPublicAddress,
                    PublicAddress = result.PublicAddress
                });
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
