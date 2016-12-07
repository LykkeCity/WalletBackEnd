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

        public static string RPCUsername
        {
            get;
            set;
        }

        public static string RPCPassword
        {
            get;
            set;
        }

        public static string RPCIPAddress
        {
            get;
            set;
        }
    }
}
