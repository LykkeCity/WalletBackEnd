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

namespace ServiceLykkeWallet
{
    class Program
    {
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
            var lykkeAccountReader = new LykkeAccountReader(settings.LykkeCredentials);

            var srvQueueReader = new SrvQueueReader(lykkeAccountReader, queueReader, queueWriter,
                log, settings.NetworkType == NetworkType.Main? Network.Main : Network.TestNet, settings.exchangePrivateKey);
            
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

    public static class SettingsReader
    {
        private static bool settingsRead = false;
        private static TheSettings settings = null;
        public class AssetDefinition
        {
            public string AssetId { get; set; }
            public string Name { get; set; }
        }
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

            public AssetDefinition[] AssetDefinitions { get; set; }
            public NetworkType NetworkType { get; set; }
            public string exchangePrivateKey { get; set; }
        }

        public static async Task<TheSettings> ReadAppSettins()
        {
            if (!settingsRead)
            {
                try
                {
                    var json = await ReadTextAsync("F:\\Lykkex\\settings.json");
                    settings = Newtonsoft.Json.JsonConvert.DeserializeObject<TheSettings>(json);
                    settingsRead = true;
                    return settings;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading settings.json file: " + ex.Message);
                    throw;
                }
            }
            else
            {
                return settings;
            }
        }

        private static async Task<string> ReadTextAsync(string filePath)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, useAsync: true))
            {
                StringBuilder sb = new StringBuilder();

                byte[] buffer = new byte[0x1000];
                int numRead;
                while ((numRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string text = Encoding.ASCII.GetString(buffer, 0, numRead);
                    sb.Append(text);
                }

                return sb.ToString();
            }
        }
    }

}
