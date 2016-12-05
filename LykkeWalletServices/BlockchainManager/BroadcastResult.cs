using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.BlockchainManager
{
    public class BroadcastResult
    {
        public BroadcastError Error
        {
            get;
            set;
        }

        public Exception BroadcastException
        {
            get;
            set;
        }
    }
}
