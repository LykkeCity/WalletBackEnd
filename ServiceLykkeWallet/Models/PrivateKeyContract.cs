using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLykkeWallet.Models
{
    public class AddPrivateKeyContract
    {
        public bool IsP2PKH
        {
            get;
            set;
        }

        public string PrivateKey
        {
            get;
            set;
        }
    }
}
