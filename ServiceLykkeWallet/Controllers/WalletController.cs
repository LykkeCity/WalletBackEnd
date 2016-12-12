using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using NBitcoin;
using ServiceLykkeWallet.Models;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;

namespace ServiceLykkeWallet.Controllers
{
    public class WalletController : ApiController
    {
        // http://localhost:8989/Wallet/IsTransactionFullyIndexed?txHash=c60ba3a02f92b3a961f8a68a2f0ade15d7b3c6c2886e5d50b6799d9d5f6f87ab 
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> IsTransactionFullyIndexed(string txHash)
        {
            IHttpActionResult result = null;
            try
            {
                var txHex = await OpenAssetsHelper.GetTransactionHex(txHash, WebSettings.ConnectionParams);

                if (txHex.Item1)
                {
                    result = BadRequest(txHex.Item2);
                }
                else
                {
                    using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                    {
                        await OpenAssetsHelper.IsTransactionFullyIndexed(new Transaction(txHex.Item3),
                            WebSettings.ConnectionParams, entities, true);
                    }
                    result = Ok();
                }
            }
            catch (Exception exp)
            {
                result = InternalServerError(exp);
            }

            await OpenAssetsHelper.SendPendingEmailsAndLogInputOutput
                (WebSettings.ConnectionString, "IsTransactionFullyIndexed:" + txHash,
                TransactionsController.ConvertResultToString(result));
            return result;
        }

        // curl -H "Content-Type: application/json" -X POST -d "{\"ClientPubKey\":\"xyz\",\"ExchangePrivateKey\":\"xyz\"}" http://localhost:8989/Wallet/AddWallet
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> AddWallet(AddWalletContract wallet)
        {
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    var exisitingRecord = (from record in entities.SegKeys
                                           where record.ClientPubKey == wallet.ClientPubKey
                                           select record).FirstOrDefault();

                    if (exisitingRecord == null)
                    {
                        entities.SegKeys.Add(new SegKey
                        {
                            ClientPubKey = wallet.ClientPubKey,
                            ExchangePrivateKey = wallet.ExchangePrivateKey
                        });
                    }
                    else
                    {
                        exisitingRecord.ExchangePrivateKey = wallet.ExchangePrivateKey;
                    }

                    await entities.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        // curl -X POST http://localhost:8989/Wallet/GetWallet -d "=0372208c7e1336b739d49332c0fa5eb92a6cba69e6a3c85ed4b681407be84b0122"
        [System.Web.Http.HttpPost]
        public async Task<IHttpActionResult> GetWallet([FromBody]string ClientPublicKey)
        {
            try
            {
                var pubkey = new PubKey(ClientPublicKey);
                var network = OpenAssetsHelper.ConvertStringNetworkToNBitcoinNetwork(WebSettings.ConnectionParams.Network);
                var clientAddress = pubkey.GetAddress(network).ToString();
                string multiSigAddress = null;

                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    var exisitingRecord = (from record in entities.SegKeys
                                           where record.ClientPubKey == ClientPublicKey
                                           select record).FirstOrDefault();

                    BitcoinSecret exchangeSecret = null;
                    if (exisitingRecord == null)
                    {
                        Key key = new Key();
                        exchangeSecret = new BitcoinSecret(key, WebSettings.ConnectionParams.BitcoinNetwork);

                        multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] {  pubkey ,
                            exchangeSecret.PubKey }).GetScriptAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();
                        entities.SegKeys.Add(new SegKey
                        {
                            ClientPubKey = ClientPublicKey,
                            ExchangePrivateKey = exchangeSecret.ToWif(),
                            ClientAddress = clientAddress,
                            MultiSigAddress = multiSigAddress
                        });
                        await entities.SaveChangesAsync();
                    }
                    else
                    {
                        exchangeSecret = new BitcoinSecret(exisitingRecord.ExchangePrivateKey);
                    }

                    var coloredMulsigAddress = BitcoinAddress.Create(multiSigAddress).ToColoredAddress().ToWif();

                    return Json(new GetWalletResult { MultiSigAddress = multiSigAddress, ColoredMultiSigAddress = coloredMulsigAddress });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
