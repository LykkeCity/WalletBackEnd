using System;
using System.IO;
using AzureRepositories;
using AzureStorage;
using Common.Log;
using Core;
using LykkeWalletServices;
using LykkeWalletServices.Accounts;
using Microsoft.Owin.Hosting;

namespace ServiceLykkeWallet
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = SettingsReader.ReadAppSettins();
            
            // ToDo - Then we go production - put here log to Database
            var log = new LogToConsole();

            // ToDo - Local Azure Emulator could not be started yet

            /*
            var queueReader = new AzureQueueReader(new AzureQueueExt(settings.InQueueConnectionString, "indata"));
            var queueWriter = new AzureQueueWriter(new AzureQueueExt(settings.OutQueueConnectionString, "outdata"));
            var lykkeAccountReader = new LykkeAccountReader(settings.LykkeCredentials);

            var srvQueueReader = new SrvQueueReader(lykkeAccountReader, queueReader, queueWriter, log);
            
            srvQueueReader.Start();
            
            Console.WriteLine("Queue reader is started");
            */
            using (WebApp.Start(settings.RestEndPoint))
            {
                Console.WriteLine($"Http Server started: {settings.RestEndPoint}");
                Console.ReadLine();
            }
        }
    }

    public static class SettingsReader
    {
        public class LykkeCredentials : ILykkeCredentials
        {
            public string PublicAddress { get; set; }
            public string PrivateKey { get; set; }
            public string CcPublicAddress { get; set; }
        }

        public class TheSettings
        {
            public string RestEndPoint { get; set; }
            public string InQueueConnectionString { get; set; }
            public string OutQueueConnectionString { get; set; }

            public LykkeCredentials LykkeCredentials { get; set; }
        }

        public static TheSettings ReadAppSettins()
        {
            try
            {
                var json = File.ReadAllText("settings.json");
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TheSettings>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading settings.json file: " + ex.Message);
                throw;
            }
        }
    }

}
