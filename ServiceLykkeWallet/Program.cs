using System;
using System.IO;
using AzureRepositories;
using AzureStorage;
using Common.Log;
using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using Microsoft.Owin.Hosting;
using System.Threading.Tasks;
using System.Text;
using Common.IocContainer;
using Core.LykkeIntegration.Services;
using LykkeIntegrationServices;
using NBitcoin;
using LykkeWalletServices.Transactions.TaskHandlers;

namespace ServiceLykkeWallet
{
    public class Program
    {
        public static SrvQueueReader srvQueueReader
        {
            get;
            private set;
        }

        public static void ConfigureAppUsingSettings(SettingsReader.TheSettings settings)
        {
            // ToDo - Then we go production - put here log to Database
            var log = new LogToConsole();

            // ToDo - Local Azure Emulator could not be started yet

            var queueReader = new AzureQueueReader(new AzureQueueExt(settings.InQueueConnectionString, "indata"));
            var queueWriter = new AzureQueueWriter(new AzureQueueExt(settings.OutQueueConnectionString, settings.OutdataQueueName));
            var emailQueueWriter = new AzureQueueExt(settings.OutQueueConnectionString, "emailsqueue");

            OpenAssetsHelper.QBitNinjaBaseUrl = settings.QBitNinjaBaseUrl;
            OpenAssetsHelper.BroadcastGroup = settings.BroadcastGroup;
            OpenAssetsHelper.FeeMultiplicationFactor = settings.FeeMultiplicationFactor;
            OpenAssetsHelper.FeeType = settings.FeeType;
            OpenAssetsHelper.EnvironmentName = settings.EnvironmentName;
            OpenAssetsHelper.PreGeneratedOutputMinimumCount = settings.PreGeneratedOutputMinimumCount;
            OpenAssetsHelper.GeneralLogger = new LogToDB();
            OpenAssetsHelper.EmailQueueWriter = emailQueueWriter;
            if (settings.SwapMinimumConfirmationNumber >= 0)
            {
                SrvSwapTask.SwapMinimumConfirmationNumber = settings.SwapMinimumConfirmationNumber;
            }
            if (settings.DefaultNumberOfRequiredConfirmations >= 0)
            {
                OpenAssetsHelper.MinimumNumberOfRequiredConfirmations = settings.DefaultNumberOfRequiredConfirmations;
            }
            if (settings.GenerateRefundingTransactionMinimumConfirmationNumber >= 0)
            {
                SrvGenerateRefundingTransactionTask.GenerateRefundingTransactionMinimumConfirmationNumber =
                    settings.GenerateRefundingTransactionMinimumConfirmationNumber;
            }
            if(settings.MaximumTransactionSendFeesInSatoshi > 0)
            {
                OpenAssetsHelper.MaximumTransactionSendFeesInSatoshi = settings.MaximumTransactionSendFeesInSatoshi;
            }
            SrvTransferTask.TransferFromPrivateWalletMinimumConfirmationNumber = settings.TransferFromPrivateWalletMinimumConfirmationNumber;
            SrvTransferTask.TransferFromMultisigWalletMinimumConfirmationNumber = settings.TransferFromMultisigWalletMinimumConfirmationNumber;

            OpenAssetsHelper.PrivateKeyWillBeSubmitted = settings.PrivateKeyWillBeSubmitted;
            GeneralHelper.ExchangePrivateKey = settings.exchangePrivateKey;
            SrvGenerateNewWalletTask.ExchangePrivateKey = settings.exchangePrivateKey;
            OpenAssetsHelper.Network = settings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet;

            GeneralHelper.UseSegKeysTable = settings.UseSegKeysTable;

            WebSettings.Assets = settings.AssetDefinitions;
            WebSettings.ConnectionParams = new OpenAssetsHelper.RPCConnectionParams
            {
                Username = settings.RPCUsername,
                Password = settings.RPCPassword,
                IpAddress = settings.RPCServerIpAddress,
                Network = settings.NetworkType.ToString()
            };
            WebSettings.ConnectionString = settings.ConnectionString;
            WebSettings.FeeAddress = settings.FeeAddress;
            WebSettings.SwapMinimumConfirmationNumber = settings.SwapMinimumConfirmationNumber;

            var logger = new LogToConsole();
            var ioc = new IoC();
            if (!settings.UseMockAsLykkeNotification)
            {
                var lykkeSettings = GeneralSettingsReader.ReadGeneralSettings<BaseSettings>(settings.LykkeSettingsConnectionString);
                ioc.BindAzureRepositories(lykkeSettings.Db, logger);
            }

            ioc.BindLykkeServices(settings.UseMockAsLykkeNotification);

            srvQueueReader = new SrvQueueReader(queueReader, queueWriter,
                log, settings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet,
                settings.AssetDefinitions, settings.RPCUsername, settings.RPCPassword,
                settings.RPCServerIpAddress, settings.ConnectionString, settings.FeeAddress, settings.FeeAddressPrivateKey,
                ioc.GetObject<IPreBroadcastHandler>());

            srvQueueReader.Start();

            var srvFeeUpdater = new SrvFeeUpdater(log);
            srvFeeUpdater.Start();

            var srvFeeReserveCleaner = new SrvFeeReserveCleaner(log, settings.ConnectionString);
            srvFeeReserveCleaner.Start();

            var srvUnsignedTransactionsUpdater = new SrvUnsignedTransactionsUpdater(log, settings.UnsignedTransactionTimeoutInMinutes, settings.UnsignedTransactionsUpdaterPeriod, settings.ConnectionString);
            srvUnsignedTransactionsUpdater.Start();


            Console.WriteLine("Queue reader is started");
        }

        static void Main(string[] args)
        {
            var settingsTask = SettingsReader.ReadAppSettins();
            settingsTask.Wait();
            var settings = settingsTask.Result;
            SrvUpdateAssetsTask.IsConfigurationEncrypted = settings.IsConfigurationEncrypted;

            if (!settings.IsConfigurationEncrypted)
            {
                ConfigureAppUsingSettings(settings);
            }

            using (WebApp.Start(settings.RestEndPoint))
            {
                Console.WriteLine($"Http Server started: {settings.RestEndPoint}");
                Console.ReadLine();
            }

            
        }
    }
}
