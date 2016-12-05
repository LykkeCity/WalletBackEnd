using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.BlockchainManager
{
    public static class LykkeBitcoinBlockchainManagerSettings
    {
        public static string ConnectionString
        {
            get;
            set;
        }

        public static Network Network
        {
            get;
            set;
        }
    }
}
