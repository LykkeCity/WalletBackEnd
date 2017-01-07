using AzureStorage;
using Core;
using LykkeWalletServices;
using LykkeWalletServices.Transactions.Responses;
using Lykkex.WalletBackend.Tests.GenerateFeeOutputs;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;
using static LykkeWalletServices.Transactions.TaskHandlers.SettingsReader;

namespace Lykkex.WalletBackend.Tests
{

    public class Settings : TheSettings
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
    }

    public class GenerateMassOutputsModel : BaseRequestModel
    {
        public string WalletAddress
        {
            get;
            set;
        }

        public string PrivateKey
        {
            get;
            set;
        }

        public int Count
        {
            get;
            set;
        }

        public float FeeAmount
        {
            get;
            set;
        }
    }

    public class BaseRequestModel
    {
        public string TransactionId
        {
            get;
            set;
        }
    }

    public class TaskTestsCommon
    {
        public static Settings Settings
        {
            get;
            set;
        }

        public static BitcoinAddress MassBitcoinHolder = Base58Data.GetFromBase58Data("mxMh2btve8BKNmPj18SkWtmaegzmM3kDtm") as BitcoinAddress;
        public const string MassBitcoinHolderPrivateKey = "cVn1XQLBwcxcaBw4FDctsYfHWqCLdQaLQz8vh5mouXw4iK6DKnPx";

        public static AzureQueueExt QueueReader = null;
        public static AzureQueueExt QueueWriter = null;

        public class TransactionResponseBase
        {
            public string TransactionId { get; set; }
            public Error Error { get; set; }
        }

        public class GenerateNewWalletResponse : TransactionResponseBase
        {
            public GenerateNewWalletTaskResult Result
            {
                get;
                set;
            }
        }

        public class GetCurrentBalanceResponse : TransactionResponseBase
        {
            public GetCurrentBalanceTaskResult Result
            {
                get;
                set;
            }
        }

        public class SwapResponse : TransactionResponseBase
        {
            public SwapTaskResult Result
            {
                get;
                set;
            }
        }

        public class CashInResponse : TransactionResponseBase
        {
            public CashInTaskResult Result
            {
                get;
                set;
            }
        }

        public class GenerateIssuerOutputsResponse : TransactionResponseBase
        {
            public GenerateMassOutputsTaskResult Result
            {
                get;
                set;
            }
        }

        public class GenerateFeeOutputsResponse : TransactionResponseBase
        {
            public GenerateMassOutputsTaskResult Result
            {
                get;
                set;
            }
        }

        public static async Task GenerateIssuersOutput(string assetName, int count, Settings settings)
        {
            GenerateIssuerOutputsModel generateIssuers = new GenerateIssuerOutputsModel
            {
                AssetName = assetName,
                FeeAmount = 0.0000273f,
                Count = count,
                PrivateKey = MassBitcoinHolderPrivateKey,
                TransactionId = Guid.NewGuid().ToString(),
                WalletAddress = MassBitcoinHolder.ToWif()
            };
            var generateIssuersOutputResponse = await CreateLykkeWalletRequestAndProcessResult<GenerateIssuerOutputsResponse>
                ("GenerateIssuerOutputs", generateIssuers, QueueReader, QueueWriter);
            await GenerateBlocks(settings, 1);
            await WaitUntillQBitNinjaHasIndexed(settings, HasTransactionIndexed,
                new string[] { generateIssuersOutputResponse.Result.TransactionHash }, null);
            await WaitUntillQBitNinjaHasIndexed(settings, HasBalanceIndexed,
                new string[] { generateIssuersOutputResponse.Result.TransactionHash }, MassBitcoinHolder.ToWif());
        }

        public async Task InitializeBitcoinNetwork(Settings settings)
        {
            await GenerateBlocks(settings, 101);

            var txId = await SendBTC(settings,
                    MassBitcoinHolder,
                    1f);
            var blocks = await GenerateBlocks(settings, 1);
            await WaitUntillQBitNinjaHasIndexed(settings, HasBalanceIndexed, new string[] { txId }, MassBitcoinHolder.ToWif());

            await GenerateIssuersOutput("TestExchangeUSD", 300, settings);
            await GenerateIssuersOutput("TestExchangeEUR", 300, settings);

            GenerateFeeOutputsModel generateFees = new GenerateFeeOutputsModel
            {
                FeeAmount = 0.00050f,
                Count = 1000,
                PrivateKey = MassBitcoinHolderPrivateKey,
                TransactionId = Guid.NewGuid().ToString(),
                WalletAddress = MassBitcoinHolder.ToWif()
            };
            var generateFeeOutputResponse = await CreateLykkeWalletRequestAndProcessResult<GenerateFeeOutputsResponse>
                ("GenerateFeeOutputs", generateFees, QueueReader, QueueWriter);
            await GenerateBlocks(settings, 1);
            await WaitUntillQBitNinjaHasIndexed(settings, HasTransactionIndexed,
                new string[] { generateFeeOutputResponse.Result.TransactionHash }, null);
            await WaitUntillQBitNinjaHasIndexed(settings, HasBalanceIndexed,
                new string[] { generateFeeOutputResponse.Result.TransactionHash }, MassBitcoinHolder.ToWif());
        }

        public static Settings ReadAppSettings()
        {
            Settings settings = new Settings();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            settings.AzureStorageEmulatorPath = config.AppSettings.Settings["AzureStorageEmulatorPath"]?.Value;
            settings.BitcoinDaemonPath = config.AppSettings.Settings["BitcoinDaemonPath"]?.Value;
            settings.BitcoinWorkingPath = config.AppSettings.Settings["BitcoinWorkingPath"]?.Value;
            settings.RegtestRPCUsername = config.AppSettings.Settings["RegtestRPCUsername"]?.Value;
            settings.RegtestRPCPassword = config.AppSettings.Settings["RegtestRPCPassword"].Value;
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
            settings.WalletBackendUrl = config.AppSettings.Settings["WalletBackendUrl"]?.Value;

            WebSettings.ConnectionString = settings.DBConnectionString;
            WebSettings.FeeAddress = config.AppSettings.Settings["FeeAddress"]?.Value;

            WebSettings.ConnectionParams = new RPCConnectionParams
            {
                IpAddress = settings.RegtestRPCIP,
                Network = settings.Network.ToString(),
                Username = settings.RegtestRPCUsername,
                Password = settings.RegtestRPCPassword
            };
            return settings;
        }

        public static LykkeExtenddedRPCClient GetRPCClient(Settings setting)
        {
            UriBuilder builder = new UriBuilder();
            builder.Host = setting.RegtestRPCIP;
            builder.Scheme = "http";
            builder.Port = setting.RegtestPort;
            var uri = builder.Uri;

            return new LykkeExtenddedRPCClient(new System.Net.NetworkCredential(setting.RegtestRPCUsername, setting.RegtestRPCPassword),
                uri);
        }

        public async static Task<bool> PerformShellCommandAndLeave(string commandName, string commandParams,
            Action<Process> processStartedCallback, string workingDiectory = null, string waitForString = null, bool redirectOutput = true)
        {
            bool exitFromMethod = false;

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            if (!string.IsNullOrEmpty(waitForString))
            {
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.RedirectStandardError = true;
                processStartInfo.UseShellExecute = false;
            }
            processStartInfo.FileName = commandName;
            processStartInfo.Arguments = commandParams;
            if (!string.IsNullOrEmpty(workingDiectory))
            {
                processStartInfo.WorkingDirectory = workingDiectory;
            }

            Process p = new Process();
            if (!string.IsNullOrEmpty(waitForString))
            {
                using (var ms = new MemoryStream())
                {
                    var sw = new StreamWriter(ms);

                    p.OutputDataReceived += (
                        (object sender, DataReceivedEventArgs e) =>
                        {
                            if (e.Data != null && e.Data.Equals(waitForString))
                            {
                                exitFromMethod = true;
                            }
                        });

                    p.ErrorDataReceived += (
                        (object sender, DataReceivedEventArgs e) =>
                        {
                            if (e.Data != null && e.Data.Equals(waitForString))
                            {
                                exitFromMethod = true;
                            }
                        });
                }
            }

            p.StartInfo = processStartInfo;
            p.Start();

            processStartedCallback(p);

            if (!string.IsNullOrEmpty(waitForString))
            {
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                var counter = 0;
                while (!exitFromMethod)
                {
                    await Task.Delay(1000);
                    counter++;
                    if (counter > 30)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return true;
            }
        }

        public static bool PerformShellCommandAndExit(string commandName, string commandParams)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = commandName;
            processStartInfo.Arguments = commandParams;

            Process p = new Process();
            using (var ms = new MemoryStream())
            {
                var sw = new StreamWriter(ms);

                p.OutputDataReceived += (
                    (object sender, DataReceivedEventArgs e) =>
                    {
                        sw.WriteLine(e.Data);
                    });

                p.ErrorDataReceived += (
                    (object sender, DataReceivedEventArgs e) =>
                    {
                        sw.Write(e.Data);
                    });

                p.StartInfo = processStartInfo;

                p.Start();
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                p.WaitForExit();
                sw.Flush();

                ms.Position = 0;
                var sr = new StreamReader(ms);
                var myStr = sr.ReadToEnd();

                if (p.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static async Task<IEnumerable<string>> GenerateBlocks(Settings settings, int count)
        {
            List<string> blockIds = new List<string>();

            for (int i = 0; i < count; i++)
            {
                var rpcClient = GetRPCClient(settings);
                var response = await rpcClient.GenerateBlocksAsync(1);
                blockIds.AddRange(response);
                await WaitUntillQBitNinjaHasIndexed(settings, HasBlockIndexed, response);
            }

            return blockIds;
        }

        public async Task<string> SendBTC(Settings settings, BitcoinAddress destination, float amount)
        {
            var rpcClient = GetRPCClient(settings);
            uint256 response = null;
            try
            {
                response = await rpcClient.SendToAddressAsync(destination, new Money((int)(amount * 100000000)));
            }
            catch (Exception exp)
            {
                return null;
            }
            await WaitUntillQBitNinjaHasIndexed(settings, HasTransactionIndexed, new string[] { response.ToString() });
            return response.ToString();
        }

        static Timer queueReaderTimer = null;
        static TaskTestsCommon()
        {
            WalletCallbackMapping = new Dictionary<string, Action<string, TransactionResponseBase>>();
            queueReaderTimer = new Timer(TimerCallback, null, 0, 250);
        }

        public static void TimerCallback(object state)
        {
            try
            {
                Task.Run(async () =>
                {
                    var reply = (TransactionResponseBase)(await QueueReader.GetMessageAsync());
                    if (reply?.TransactionId != null)
                    {
                        if (WalletCallbackMapping.Keys.Contains(reply?.TransactionId))
                        {
                            WalletCallbackMapping[reply.TransactionId](reply.TransactionId, reply);
                        }
                    }
                });
            }
            catch (Exception exp)
            {

            }
        }

        public static IDictionary<string, Action<string, TransactionResponseBase>> WalletCallbackMapping
        {
            get;
            set;
        }

        public static TransactionResponseBase LykkeWalletCallbackResult
        {
            get;
            set;
        }

        public static bool LykkeWalletCallbackResultArrived
        {
            get;
            set;
        }


        public static void GeneralLykkeWalletCallback(string transactionId, TransactionResponseBase result)
        {
            LykkeWalletCallbackResult = result;
            LykkeWalletCallbackResultArrived = true;
        }

        public async static Task<T> CreateLykkeWalletRequestAndProcessResult<T>(string opName, BaseRequestModel param,
            AzureQueueExt QueueReader, AzureQueueExt QueueWriter, Action<string, TransactionResponseBase> returnCallback = null) where T : TransactionResponseBase
        {
            LykkeWalletCallbackResult = null;
            LykkeWalletCallbackResultArrived = false;
            if (returnCallback == null)
            {
                returnCallback = GeneralLykkeWalletCallback;
            }

            WalletCallbackMapping.Add(param.TransactionId, returnCallback);

            var requestString = string.Format("{0}:{1}", opName, Newtonsoft.Json.JsonConvert.SerializeObject(param));
            await QueueWriter.PutMessageAsync(requestString);

            if (returnCallback == GeneralLykkeWalletCallback)
            {
                while (!LykkeWalletCallbackResultArrived)
                {
                    Thread.Sleep(300);
                }

                if (LykkeWalletCallbackResult == null)
                {
                    throw new Exception(string.Format("{0} reply is null.", opName));
                }
                if (LykkeWalletCallbackResult.Error != null)
                {
                    throw new Exception(string.Format("{0}: {1}", opName,
                         LykkeWalletCallbackResult.Error.Code.ToString() + ": " + LykkeWalletCallbackResult.Error.Message));
                }

                return LykkeWalletCallbackResult as T;
            }
            else
            {
                return null;
            }
        }

        /*
        public async static Task<T> GetReturnReply<T>(AzureQueueExt QueueReader)
        {
            object message = null;
            int count = 0;
            var delayTimeInMilisecond = 100;
            while (true)
            {
                message = await QueueReader.GetMessageAsync();
                if (message != null)
                {
                    return (T)message;
                }
                count++;
                await Task.Delay(delayTimeInMilisecond);
                if (count > (60 * 1000 / delayTimeInMilisecond))
                {
                    return default(T);
                }
            }
        }
        */

        public static async Task SwapAssets(string guid, string address01, string asset01, double amount01,
            string address02, string asset02, double amount02,
            Action<string, TransactionResponseBase> callback = null)
        {
            SwapRequestModel swap = new SwapRequestModel
            {
                TransactionId = guid,
                MultisigCustomer1 = address01,
                MultisigCustomer2 = address02,
                Asset1 = asset01,
                Asset2 = asset02,
                Amount1 = amount01,
                Amount2 = amount02
            };

            await CreateLykkeWalletRequestAndProcessResult<SwapResponse>
                ("Swap", swap, QueueReader, QueueWriter, callback);
        }

        public static async Task<string> CashinToAddress(string destAddress, string asset, double amount)
        {
            CashInRequestModel cashin = new CashInRequestModel
            {
                TransactionId = Guid.NewGuid().ToString(),
                MultisigAddress = destAddress,
                Amount = amount,
                Currency = asset
            };
            var reply = await CreateLykkeWalletRequestAndProcessResult<CashInResponse>
                ("CashIn", cashin, QueueReader, QueueWriter);
            await GenerateBlocks(Settings, 1);
            await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                new string[] { reply.Result.TransactionHash }, null);
            await OpenAssetsHelper.WaitUntillQBitNinjaHasIndexed(Settings, HasBalanceIndexed,
                new string[] { reply.Result.TransactionHash }, destAddress);

            return reply.TransactionId;
        }

        public async static Task<GenerateNewWalletTaskResult> GenerateNewWallet
            (AzureQueueExt QueueReader, AzureQueueExt QueueWriter, Action<string, TransactionResponseBase> callback = null, Guid txId = default(Guid))
        {
            if (txId == default(Guid))
            {
                txId = Guid.NewGuid();
            }
            GenerateNewWalletModel generateNewWallet = new GenerateNewWalletModel { TransactionId = txId.ToString() };

            var reply = await CreateLykkeWalletRequestAndProcessResult<GenerateNewWalletResponse>("GenerateNewWallet", generateNewWallet,
                QueueReader, QueueWriter, callback);

            return reply?.Result;
        }
    }
}
