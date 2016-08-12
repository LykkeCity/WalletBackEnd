﻿using Core;
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

                    if(exisitingRecord == null)
                    {
                        entities.SegKeys.Add(new SegKey { ClientPubKey = wallet.ClientPubKey,
                            ExchangePrivateKey = wallet.ExchangePrivateKey });
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
                        entities.SegKeys.Add(new SegKey
                        {
                            ClientPubKey = ClientPublicKey,
                            ExchangePrivateKey =  exchangeSecret.ToWif()
                        });
                        await entities.SaveChangesAsync();
                    }
                    else
                    {
                        exchangeSecret = new BitcoinSecret(exisitingRecord.ExchangePrivateKey);
                    }

                    var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { new PubKey(ClientPublicKey) ,
                        exchangeSecret.PubKey });
                    var multiSigAddressStorage = multiSigAddress.GetScriptAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();
                    var coloredMulsigAddress = BitcoinAddress.Create(multiSigAddressStorage).ToColoredAddress().ToWif();

                    return Json(new GetWalletResult { MultiSigAddress = multiSigAddressStorage, ColoredMultiSigAddress = coloredMulsigAddress });
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}