using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using LykkeWalletServices.Transactions.TaskHandlers;
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
        // curl http://localhost:8989/General/IsAlive
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> IsAlive()
        {
            var settings = await SettingsReader.ReadAppSettins();
            if (settings.IsConfigurationEncrypted && SrvUpdateAssetsTask.EncryptionKey == null)
            {
                return InternalServerError(new Exception("Decryption key has not been submitted yet."));
            }

            try
            {
                using (SqlexpressLykkeEntities entities =
                    new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    // Checking if DB is 
                    long count = entities.InputOutputMessageLogs.Count();

                    // Checking if Blockchain explorer is accessable
                    // The address is a random one taken from blockchain, does not have a special meaning
                    var testAddress = (WebSettings.ConnectionParams.BitcoinNetwork == Network.Main ?
                        "1Ge8w4BRYnxg96pCQftHTwquKreHxCKzBJ" : "n4n59uCrRgHTcA2Nunw3vjxbthVBVsUKFN");
                    var walletOutputs = await OpenAssetsHelper.GetWalletOutputs
                        (testAddress, WebSettings.ConnectionParams.BitcoinNetwork, entities);

                    if (walletOutputs.Item2)
                    {
                        return BadRequest(walletOutputs.Item3);
                    }
                }
            }
            catch (Exception exp)
            {
                return InternalServerError(exp);
            }
            return Ok();
        }

        [System.Web.Http.HttpGet]
        public IHttpActionResult HasTripleDESKeySubmitted()
        {
            if (SrvUpdateAssetsTask.EncryptionKey == null)
            {
                return Ok(false);
            }
            else
            {
                return Ok(true);
            }
        }

        // curl http://localhost:8989/General/GetNewTripleDESIVKey
        [System.Web.Http.HttpGet]
        public IHttpActionResult GetNewTripleDESIVKey()
        {
            return Ok(BitConverter.ToString(TripleDESManaged.GetNewIVKey()).Replace("-", string.Empty));
        }

        // curl -X GET "http://localhost:8989/General/EncryptUsingTripleDES?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7&message=Hello\""
        [System.Web.Http.HttpGet]
        public IHttpActionResult EncryptUsingTripleDES(string key, string message)
        {
            return Ok(TripleDESManaged.Encrypt(key, message));
        }

        // curl -X GET "http://localhost:8989/General/DecryptUsingTripleDES?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7&encrypted=9436D1DC92F8232C"
        [System.Web.Http.HttpGet]
        public IHttpActionResult DecryptUsingTripleDES(string key, string encrypted)
        {
            return Ok(TripleDESManaged.Decrypt(key, encrypted));
        }

        // curl -X GET "http://localhost:8989/General/DecodeSettingsUsingTheProvidedPrivateKey?key=1F396986D834792CB3A530B37086E690400A2C426140DE9DF4C4CF8593D802D7"
        [System.Web.Http.HttpGet]
        public async Task<IHttpActionResult> DecodeSettingsUsingTheProvidedPrivateKey(string key)
        {
            var settings = await SettingsReader.ReadAppSettins();
            if (!settings.IsConfigurationEncrypted)
            {
                return BadRequest("Configuration is not encrypted.");
            }
            if (SrvUpdateAssetsTask.EncryptionKey != null)
            {
                return BadRequest("Encryption key has been submitted previously.");
            }

            settings.InQueueConnectionString = TripleDESManaged.Decrypt(key, settings.InQueueConnectionString);
            settings.OutQueueConnectionString = TripleDESManaged.Decrypt(key, settings.OutQueueConnectionString);
            settings.ConnectionString = TripleDESManaged.Decrypt(key, settings.ConnectionString);
            settings.exchangePrivateKey = TripleDESManaged.Decrypt(key, settings.exchangePrivateKey);
            settings.FeeAddressPrivateKey = TripleDESManaged.Decrypt(key, settings.FeeAddressPrivateKey);
            settings.LykkeSettingsConnectionString = TripleDESManaged.Decrypt(key, settings.LykkeSettingsConnectionString);
            for (int i = 0; i < settings.AssetDefinitions.Length; i++)
            {
                if (!string.IsNullOrEmpty(settings.AssetDefinitions[i].PrivateKey))
                {
                    settings.AssetDefinitions[i].PrivateKey = TripleDESManaged.Decrypt(key, settings.AssetDefinitions[i].PrivateKey);
                }
            }

            Program.ConfigureAppUsingSettings(settings);

            SrvUpdateAssetsTask.EncryptionKey = key;

            return Ok();
        }

        // This should respond http://localhost:8989/General/GetPublicKeyFromPrivateKey?privatekey=cQKNnKS7TUFPdVc4muGXq8X9h5dxuGyYBSbnFUUuv9NVsLDNFP51
        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetPublicKeyFromPrivateKey(string privatekey)
        {
            BitcoinSecret secret = BitcoinSecret.GetFromBase58Data(privatekey) as BitcoinSecret;

            var result = new HttpResponseMessage(HttpStatusCode.Accepted);
            result.Content = new StringContent(secret.PubKey.ToHex());

            return result;
        }

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

#if DEBUG
        // curl localhost:8989/General/SetFeeRate?feeRate=11000
        [System.Web.Http.HttpGet]
        public IHttpActionResult SetFeeRate(uint feeRate)
        {
            if (feeRate < 10000 || feeRate > 60000)
            {
                return BadRequest("While setting manually feeRate should be between 10000 and 60000.");
            }
            else
            {
                OpenAssetsHelper.TransactionSendFeesInSatoshi = feeRate;
                return Ok("Fee set.");
            }
        }
#endif
        [System.Web.Http.HttpGet]
        public HttpResponseMessage GetWallets()
        {
            StringBuilder builder = new StringBuilder();

            foreach (var item in OpenAssetsHelper.P2PKHDictionary)
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
