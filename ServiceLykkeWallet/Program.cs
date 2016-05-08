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

        static void Main(string[] args)
        {
            var settingsTask = SettingsReader.ReadAppSettins();
            settingsTask.Wait();
            var settings = settingsTask.Result;

            // ToDo - Then we go production - put here log to Database
            var log = new LogToConsole();

            // ToDo - Local Azure Emulator could not be started yet

            var queueReader = new AzureQueueReader(new AzureQueueExt(settings.InQueueConnectionString, "indata"));
            var queueWriter = new AzureQueueWriter(new AzureQueueExt(settings.OutQueueConnectionString, "outdata"));
            var emailQueueWriter = new AzureQueueExt(settings.OutQueueConnectionString, "emailsqueue");
            var lykkeAccountReader = new LykkeAccountReader(settings.LykkeCredentials);

            OpenAssetsHelper.QBitNinjaBaseUrl = settings.QBitNinjaBaseUrl;
            OpenAssetsHelper.PreGeneratedOutputMinimumCount = settings.PreGeneratedOutputMinimumCount;
            OpenAssetsHelper.EmailQueueWriter = emailQueueWriter;

            srvQueueReader = new SrvQueueReader(lykkeAccountReader, queueReader, queueWriter,
                log, settings.NetworkType == NetworkType.Main ? Network.Main : Network.TestNet,
                settings.exchangePrivateKey, settings.AssetDefinitions, settings.RPCUsername, settings.RPCPassword,
                settings.RPCServerIpAddress, settings.ConnectionString, settings.FeeAddress, settings.FeeAddressPrivateKey);

            srvQueueReader.Start();

            Console.WriteLine("Queue reader is started");

            /*
            using (WebApp.Start(settings.RestEndPoint))
            {
                Console.WriteLine($"Http Server started: {settings.RestEndPoint}");
            }
            */
            Console.ReadLine();
        }
    }
}
