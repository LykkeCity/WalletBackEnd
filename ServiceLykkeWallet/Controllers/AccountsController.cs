using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using LykkeWalletServices.Accounts;

namespace ServiceLykkeWallet.Controllers
{
    public class AccountsController : ApiController
    {

        [HttpGet]
        public IHttpActionResult GenerateAddresses()
        {
            var srvAccountGenerator = new SrvAccountGenerator();
            var result = srvAccountGenerator.GenerateAccount();
            return Json(
                new 
                {
                   privateKey = result.PrivateKey,
                   multisigPublicAddress = result.MultisigPublicAddress,
                   ccPublicAddress = result.CcPublicAddress,
                   publicAddress = result.PublicAddress,

                }

                );
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
