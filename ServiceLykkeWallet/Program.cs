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
using Castle.Windsor;
using Castle.MicroKernel.Registration;
using Castle.DynamicProxy;
using Castle.Core;
using Common;

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

            AzureQueueReader queueReader = null;
            AzureQueueWriter queueWriter = null;
            if (Container == null)
            {
                queueReader = new AzureQueueReader(new AzureQueueExt(settings.InQueueConnectionString, "indata"));
                queueWriter = new AzureQueueWriter(new AzureQueueExt(settings.OutQueueConnectionString, settings.OutdataQueueName));
            }
            else
            {
                queueReader = new AzureQueueReader(Container.Resolve<IQueueExt>(new { conectionString = settings.InQueueConnectionString, queueName = "indata", types = new QueueType[] { } }));
                queueWriter = new AzureQueueWriter(Container.Resolve<IQueueExt>(new { conectionString = settings.OutQueueConnectionString, queueName = settings.OutdataQueueName, types = new QueueType[] { } }));
            }
            
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
            if (settings.MaximumTransactionSendFeesInSatoshi > 0)
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
            WebSettings.UseSegKeysTable = settings.UseSegKeysTable;

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
                settings.RPCServerIpAddress, settings.ConnectionString, settings.FeeAddress,
                settings.FeeAddressPrivateKey, ioc.GetObject<IPreBroadcastHandler>(),
                settings.QueueReaderIntervalInMiliseconds);

            srvQueueReader.Start();

            var srvFeeUpdater = new SrvFeeUpdater(log);
            srvFeeUpdater.Start();

            var srvFeeReserveCleaner = new SrvFeeReserveCleaner(log, settings.ConnectionString,
                settings.FeeReserveCleanerTimerPeriodInSeconds, settings.FeeReserveCleanerNumberOfFeesToCleanEachTime);
            srvFeeReserveCleaner.Start();

            var srvOffchainReserveCleaner = new SrvOffchainReserveCleaner(log, settings.ConnectionString);
            srvOffchainReserveCleaner.Start();

            var srvUnsignedTransactionsUpdater = new SrvUnsignedTransactionsUpdater(log, settings.UnsignedTransactionTimeoutInMinutes, settings.UnsignedTransactionsUpdaterPeriod, settings.ConnectionString);
            srvUnsignedTransactionsUpdater.Start();



            Console.WriteLine("Queue reader is started");
        }

        public static WindsorContainer Container
        {
            get;
            set;
        }

        static void Main(string[] args)
        {
            using (WindsorContainer container = new WindsorContainer())
            {
                container.Register(
                    Component.For<IInterceptor>()
                    .ImplementedBy<AsyncLoggingWithExceptionInterceptor>()
                    .Named("loggingInterceptor").LifeStyle.Transient);

                container.Register(
                    Component.For<ILog>()
                    .ImplementedBy<LykkeInterceptLogger>().LifeStyle.Transient);

                container.Register(
                    Component.For<IExceptionHandler>()
                    .ImplementedBy<DefaultExceptionHandler>().LifeStyle.Transient);

                container.Register(
                    Component.For<IMethodSelectorForLogging>()
                    .ImplementedBy<QueueMethodSelectorForLogging>().LifeStyle.Transient);

                container.Register(
                    Component.For<IQueueExt>()
                    .ImplementedBy<AzureQueueExt>()
                 .Interceptors(InterceptorReference.ForKey("loggingInterceptor")).Anywhere.LifeStyle.Transient);

                Program.Container = container;

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
}
