using LykkeWalletServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public static class WebSettings
    {
        public static OpenAssetsHelper.RPCConnectionParams ConnectionParams
        {
            get;
            set;
        }

        public static Core.AssetDefinition[] Assets
        {
            get;
            set;
        }

        public static string ExchangePrivateKey
        {
            get;
            set;
        }

        public static string FeeAddress
        {
            get;
            set;
        }

        public static string ConnectionString
        {
            get;
            set;
        }
    }
}
