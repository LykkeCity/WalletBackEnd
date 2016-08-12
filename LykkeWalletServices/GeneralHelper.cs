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
        public static string ExchangePrivateKey
        {
            get;
            set;
        }

        public static BitcoinSecret GetExchangePrivateKey(this PubKey clientPubKey)
        {
            return new BitcoinSecret(ExchangePrivateKey);
        }
    }
}
