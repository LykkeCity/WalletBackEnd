using Core;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public static class SettingsReader
    {
        private static bool settingsRead = false;
        private static TheSettings settings = null;

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

            public string ConnectionString { get; set; }

            public LykkeCredentials LykkeCredentials { get; set; }

            public AssetDefinition[] AssetDefinitions { get; set; }
            public NetworkType NetworkType { get; set; }
            public string exchangePrivateKey { get; set; }
            public string RPCUsername { get; set; }
            public string RPCPassword { get; set; }
            public string RPCServerIpAddress { get; set; }
            public string FeeAddress { get; set; }
            public string FeeAddressPrivateKey { get; set; }

            public string QBitNinjaBaseUrl { get; set; }
            public int PreGeneratedOutputMinimumCount { get; set; }

            public string LykkeJobsUrl { get; set; }

            [DefaultValue(400)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int BroadcastGroup
            {
                get;
                set;
            }

            [DefaultValue(0)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int SwapMinimumConfirmationNumber
            {
                get;
                set;
            }

            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int DefaultNumberOfRequiredConfirmations
            {
                get;
                set;
            }
        }

        public static async Task<TheSettings> ReadAppSettins(bool logToConsole = true)
        {
            if (!settingsRead)
            {
                try
                {
                    var json = await ReadTextAsync(SrvUpdateAssetsTask.SETTINGSFILEPATH);
                    settings = Newtonsoft.Json.JsonConvert.DeserializeObject<TheSettings>(json);
                    settingsRead = true;
                    return settings;
                }
                catch (Exception ex)
                {
                    if (logToConsole)
                    {
                        Console.WriteLine("Error reading settings.json file: " + ex.Message);
                    }
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

    // Sample request: UpdateAssets:{"TransactionId":"10","Assets": [{ "AssetId": "oDmVkVUHnrdSKASFWuHy6hxqTWFc9vdL9d", "AssetAddress": "n2JMZcG3dKuRN4c8K89TBwwwDpHshppQUr", "Name": "TestExchangeUSD", "PrivateKey": "xxx", "DefinitionUrl": "https://www.cpr.sm/-KDPVKLTlL","Divisibility": 2 }, { "AssetId": "oZTd8ZfoyRPkYFhbeLXvordpcpND2YpqPg", "AssetAddress": "n4XdhcAWoRBesY2gy5hnF6ht31rLG19kqy", "Name": "TestExchangeEUR","PrivateKey": "xxx","DefinitionUrl": "https://www.cpr.sm/SBi9SeNlyB","Divisibility": 2}]}
    // Sample response: UpdateAssets:{"TransactionId":"10","Result":{"Success":true},"Error":null}
    public class SrvUpdateAssetsTask
    {
#if DEBUG
        public const string SETTINGSFILEPATH = "F:\\Lykkex\\settings.json";
#else
        public const string SETTINGSFILEPATH = "settings.json";
#endif

        SrvQueueReader QueueReaderInstance
        {
            get;
            set;
        }

        public SrvUpdateAssetsTask(SrvQueueReader queueReaderInstance)
        {
            QueueReaderInstance = queueReaderInstance;
        }

        public async Task<Tuple<UpdateAssetsTaskResult, Error>> ExecuteTask(TaskToDoUpdateAssets data)
        {
            UpdateAssetsTaskResult result = null;
            Error error = null;
            try
            {
                // Updating the settings file
                var settings = await SettingsReader.ReadAppSettins(false);
                settings.AssetDefinitions = data.Assets;
                using (StreamWriter writer = new StreamWriter(SETTINGSFILEPATH))
                {
                    await writer.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented));
                    writer.Flush();
                }

                // Updating the currently used settings
                QueueReaderInstance._assets = data.Assets;

                result = new UpdateAssetsTaskResult();
                result.Success = true;
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<UpdateAssetsTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoUpdateAssets data, Func<Tuple<UpdateAssetsTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
