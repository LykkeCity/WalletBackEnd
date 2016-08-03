using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface ILykkeCredentials
    {
        string PublicAddress { get; }
        string PrivateKey { get; }
        string CcPublicAddress { get; } 
    }

    public class DbSettings
    {
        public string ClientPersonalInfoConnString { get; set; }
        public string BalancesInfoConnString { get; set; }
        public string ALimitOrdersConnString { get; set; }
        public string HLimitOrdersConnString { get; set; }
        public string HMarketOrdersConnString { get; set; }
        public string HTradesConnString { get; set; }
        public string HLiquidityConnString { get; set; }
        public string BackOfficeConnString { get; set; }
        public string BitCoinQueueConnectionString { get; set; }
        public string DictsConnString { get; set; }
        public string LogsConnString { get; set; }
    }

    public class BaseSettings
    {
        public DbSettings Db { get; set; }
    }
}
