using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public static class GeneralHelper
    {
        public static bool UseSegKeysTable
        {
            get;
            set;
        }

        public static string ExchangePrivateKey
        {
            get;
            set;
        }

        public static BitcoinSecret GetExchangePrivateKey(this PubKey clientPubKey,
            SqlexpressLykkeEntities entities, string connectionString = null)
        {
            if (UseSegKeysTable)
            {
                string exchangePrivateKey = null;
                var clientPubKeyHex = clientPubKey.ToHex();
                if (entities != null)
                {
                    exchangePrivateKey = (from record in entities.SegKeys
                                          where record.ClientPubKey == clientPubKeyHex
                                          select record.ExchangePrivateKey).FirstOrDefault();
                }
                else
                {
                    using (entities = new SqlexpressLykkeEntities(connectionString))
                    {
                        exchangePrivateKey = (from record in entities.SegKeys
                                              where record.ClientPubKey == clientPubKeyHex
                                              select record.ExchangePrivateKey).FirstOrDefault();

                    }
                }

                if (exchangePrivateKey == null)
                {
                    throw new ArgumentOutOfRangeException("ClientPubKey", string.Format("The provided public key: {0} is not present in persistence.", clientPubKey));
                }
                else
                {
                    return new BitcoinSecret(exchangePrivateKey);
                }
            }
            else
            {
                return new BitcoinSecret(ExchangePrivateKey);
            }
        }
    }
}
