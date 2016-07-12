using NBitcoin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonBitcoinTests
{
    public class Settings
    {
        public string AzureStorageEmulatorPath
        {
            get;
            set;
        }

        public string BitcoinDaemonPath
        {
            get;
            set;
        }

        public string BitcoinWorkingPath
        {
            get;
            set;
        }

        public string RegtestRPCUsername
        {
            get;
            set;
        }

        public string RegtestRPCPassword
        {
            get;
            set;
        }

        public string RegtestRPCIP
        {
            get;
            set;
        }

        public int RegtestPort
        {
            get;
            set;
        }

        public string QBitNinjaListenerConsolePath
        {
            get;
            set;
        }

        public string WalletBackendExecutablePath
        {
            get;
            set;
        }

        public string InQueueConnectionString
        {
            get;
            set;
        }

        public string OutQueueConnectionString
        {
            get;
            set;
        }

        public string DBConnectionString
        {
            get;
            set;
        }

        public Network Network
        {
            get;
            set;
        }

        public string ExchangePrivateKey
        {
            get;
            set;
        }

        public string QBitNinjaBaseUrl
        {
            get;
            set;
        }
    }
    public static class SettingsReader
    {
        public static Settings Settings
        {
            get;
            set;
        }
        static SettingsReader()
        {
            Settings = ReadAppSettings();
        }
        public static Settings ReadAppSettings()
        {
            Settings settings = new Settings();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            settings.AzureStorageEmulatorPath = config.AppSettings.Settings["AzureStorageEmulatorPath"]?.Value;
            settings.BitcoinDaemonPath = config.AppSettings.Settings["BitcoinDaemonPath"]?.Value;
            settings.BitcoinWorkingPath = config.AppSettings.Settings["BitcoinWorkingPath"]?.Value;
            settings.RegtestRPCUsername = config.AppSettings.Settings["RegtestRPCUsername"]?.Value;
            settings.RegtestRPCPassword = config.AppSettings.Settings["RegtestRPCPassword"]?.Value;
            settings.RegtestRPCIP = config.AppSettings.Settings["RegtestRPCIP"]?.Value;
            settings.RegtestPort = int.Parse(config.AppSettings.Settings["RegtestPort"]?.Value);
            settings.QBitNinjaListenerConsolePath = config.AppSettings.Settings["QBitNinjaListenerConsolePath"]?.Value;
            settings.WalletBackendExecutablePath = config.AppSettings.Settings["WalletBackendExecutablePath"]?.Value;
            settings.InQueueConnectionString = config.AppSettings.Settings["InQueueConnectionString"]?.Value;
            settings.OutQueueConnectionString = config.AppSettings.Settings["OutQueueConnectionString"]?.Value;
            settings.DBConnectionString = config.AppSettings.Settings["DBConnectionString"]?.Value;
            settings.ExchangePrivateKey = config.AppSettings.Settings["ExchangePrivateKey"]?.Value;
            settings.Network = config.AppSettings.Settings["Network"].Value.ToLower().Equals("main") ? NBitcoin.Network.Main : NBitcoin.Network.TestNet;
            settings.QBitNinjaBaseUrl = config.AppSettings.Settings["QBitNinjaBaseUrl"]?.Value;
            return settings;
        }
    }
}
