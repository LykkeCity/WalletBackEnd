using Core;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using LykkeWalletServices.TimerServices;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public static class SettingsReader
    {
        private static bool settingsRead = false;
        private static TheSettings settings = null;

        [Serializable]
        public class TheSettings
        {
            public string RestEndPoint { get; set; }
            public string InQueueConnectionString { get; set; }
            public string OutQueueConnectionString { get; set; }

            public string ConnectionString { get; set; }
            public string LykkeSettingsConnectionString { get; set; }

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

            [DefaultValue("outdata")]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public string OutdataQueueName
            {
                get;
                set;
            }

            [DefaultValue(null)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public string EnvironmentName
            {
                get;
                set;
            }

            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public uint FeeMultiplicationFactor
            {
                get;
                set;
            }

            [DefaultValue(FeeType21co.HalfHourFee)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public FeeType21co FeeType
            {
                get;
                set;
            }

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

            [DefaultValue(0)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int TransferFromPrivateWalletMinimumConfirmationNumber
            {
                get;
                set;
            }

            [DefaultValue(0)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int TransferFromMultisigWalletMinimumConfirmationNumber
            {
                get;
                set;
            }

            [DefaultValue(1)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int GenerateRefundingTransactionMinimumConfirmationNumber
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

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool PrivateKeyWillBeSubmitted
            {
                get;
                set;
            }

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool UseMockAsLykkeNotification
            {
                get;
                set;
            }

            [DefaultValue(10 * 60 * 1000)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int UnsignedTransactionsUpdaterPeriod
            {
                get;
                set;
            }

            [DefaultValue(5)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public int UnsignedTransactionTimeoutInMinutes
            {
                get;
                set;
            }

            [DefaultValue(true)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool UseSegKeysTable
            {
                get;
                set;
            }

            [DefaultValue(false)]
            [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
            public bool IsConfigurationEncrypted
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
        public const string SETTINGSFILEPATH = "D:\\Lykkex\\settings.json";
#else
        public const string SETTINGSFILEPATH = "settings.json";
#endif
        public static bool IsConfigurationEncrypted { get; set; }
        public static string EncryptionKey { get; set; }

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
                foreach (var item in data.Assets)
                {
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        continue;
                    }
                    else
                    {
                        var matchedAsset = settings.AssetDefinitions.Where(c => c.Name.Equals(item.Name)).FirstOrDefault();
                        if (matchedAsset != null)
                        {
                            matchedAsset.AssetAddress = item.AssetAddress ?? matchedAsset.AssetAddress;
                            matchedAsset.AssetId = item.AssetId ?? matchedAsset.AssetId;
                            matchedAsset.DefinitionUrl = item.DefinitionUrl ?? matchedAsset.DefinitionUrl;
                            matchedAsset.Divisibility = item.Divisibility ?? matchedAsset.Divisibility;
                            matchedAsset.PrivateKey = item.PrivateKey ?? matchedAsset.PrivateKey;
                        }
                        else
                        {
                            settings.AssetDefinitions = settings.AssetDefinitions.Concat(new AssetDefinition[] { item }).ToArray();
                        }
                    }
                }

                var settingsToWrite = OpenAssetsHelper.DeepClone(settings);
                if (settingsToWrite.IsConfigurationEncrypted)
                {
                    settingsToWrite.InQueueConnectionString = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.InQueueConnectionString);
                    settingsToWrite.OutQueueConnectionString = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.OutQueueConnectionString);
                    settingsToWrite.ConnectionString = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.ConnectionString);
                    settingsToWrite.exchangePrivateKey = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.exchangePrivateKey);
                    settingsToWrite.FeeAddressPrivateKey = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.FeeAddressPrivateKey);
                    for (int i = 0; i < settings.AssetDefinitions.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(settings.AssetDefinitions[i].PrivateKey))
                        {
                            settingsToWrite.AssetDefinitions[i].PrivateKey = TripleDESManaged.Encrypt(EncryptionKey, settingsToWrite.AssetDefinitions[i].PrivateKey);
                        }
                    }
                }

                using (StreamWriter writer = new StreamWriter(SETTINGSFILEPATH))
                {
                    await writer.WriteAsync(Newtonsoft.Json.JsonConvert.SerializeObject(settingsToWrite, Newtonsoft.Json.Formatting.Indented));
                    writer.Flush();
                }

                // Updating the currently used settings
                QueueReaderInstance._assets = settings.AssetDefinitions;
                WebSettings.Assets = settings.AssetDefinitions;

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
