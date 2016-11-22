using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLykkeWallet.Models
{
    public class AddWalletContract
    {
        public string ClientPubKey
        {
            get;
            set;
        }

        public string ExchangePrivateKey
        {
            get;
            set;
        }
    }

    public class GetWalletResult
    {
        public string MultiSigAddress { get; set; }
        public string ColoredMultiSigAddress { get; set; }
    }
}
