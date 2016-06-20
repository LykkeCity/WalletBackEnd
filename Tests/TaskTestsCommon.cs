using AzureRepositories;
using AzureStorage;
using Common;
using NBitcoin;
using NBitcoin.RPC;
using NUnit.Framework;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Core;
using LykkeWalletServices;
using System.Linq;
using Lykkex.WalletBackend.Tests.CashIn;
using Lykkex.WalletBackend.Tests.GenerateIssuerOutputs;
using System.Threading;
using Lykkex.WalletBackend.Tests.GenerateFeeOutputs;

namespace Lykkex.WalletBackend.Tests
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
        public Settings Settings
        {
            get;
            set;
        }

        public static BitcoinAddress MassBitcoinHolder = Base58Data.GetFromBase58Data("mxMh2btve8BKNmPj18SkWtmaegzmM3kDtm") as BitcoinAddress;
        public const string MassBitcoinHolderPrivateKey = "cVn1XQLBwcxcaBw4FDctsYfHWqCLdQaLQz8vh5mouXw4iK6DKnPx";

        public AzureQueueExt QueueReader = null;
        public AzureQueueExt QueueWriter = null;

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

        public static void ClearDB(Settings settings)
        {
            using (SqlexpressLykkeEntities entities
                = new SqlexpressLykkeEntities(settings.DBConnectionString))
            {
                entities.EmailMessages.RemoveRange(entities.EmailMessages.Select(c => c));
                entities.ExchangeRequests.RemoveRange(entities.ExchangeRequests.Select(c => c));
                entities.RefundedOutputs.RemoveRange(entities.RefundedOutputs.Select(c => c));
                entities.RefundTransactions.RemoveRange(entities.RefundTransactions.Select(c => c));
                entities.SentTransactions.RemoveRange(entities.SentTransactions.Select(c => c));
                entities.PreGeneratedOutputs.RemoveRange(entities.PreGeneratedOutputs.Select(c => c));
                entities.SpentOutputs.RemoveRange(entities.SpentOutputs.Select(c => c));
                entities.KeyStorages.RemoveRange(entities.KeyStorages.Select(c => c));
                entities.SaveChanges();
            }
        }

        public void InitializeBitcoinNetwork(Settings settings)
        {
            SendBTC(settings,
                    MassBitcoinHolder,
                    0.5f).Wait();
            GenerateBlocks(settings, 1).Wait();
            Thread.Sleep(10000);

            GenerateIssuerOutputsModel generateIssuers = new GenerateIssuerOutputsModel { AssetName = "TestExchangeUSD",
                FeeAmount = 0.0000273f, Count = 50, PrivateKey = MassBitcoinHolderPrivateKey,
                TransactionId = "10", WalletAddress = MassBitcoinHolder.ToWif() };
            CreateLykkeWalletRequestAndProcessResult<GenerateIssuerOutputsResponse>("GenerateIssuerOutputs", generateIssuers, QueueReader, QueueWriter);
            GenerateBlocks(settings, 1).Wait();
            Thread.Sleep(10000);

            GenerateFeeOutputsModel generateFees = new GenerateFeeOutputsModel
            {
                FeeAmount = 0.00015f,
                Count = 50,
                PrivateKey = MassBitcoinHolderPrivateKey,
                TransactionId = "10",
                WalletAddress = MassBitcoinHolder.ToWif()
            };
            CreateLykkeWalletRequestAndProcessResult<GenerateFeeOutputsResponse>("GenerateFeeOutputs", generateFees, QueueReader, QueueWriter);
            GenerateBlocks(settings, 1).Wait();
        }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            Settings = ReadAppSettings();

            ClearDB(Settings);

            Startup(Settings);
            GenerateBlocks(Settings, 101).Wait();
            QueueReader = new AzureQueueExt(Settings.InQueueConnectionString, "outdata");
            QueueWriter = new AzureQueueExt(Settings.OutQueueConnectionString, "indata");

            QueueReader.RegisterTypes(
                QueueType.Create("GenerateNewWallet", typeof(GenerateNewWalletResponse)),
                QueueType.Create("CashIn", typeof(CashInResponse)),
                QueueType.Create("OrdinaryCashIn", typeof(TaskToDoOrdinaryCashIn)),
                QueueType.Create("CashOut", typeof(TaskToDoCashOut)),
                QueueType.Create("CashOutSeparateSignatures", typeof(TaskToDoCashOutSeparateSignatures)),
                QueueType.Create("OrdinaryCashOut", typeof(TaskToDoOrdinaryCashOut)),
                QueueType.Create("GetCurrentBalance", typeof(GetCurrentBalanceResponse)),
                QueueType.Create("Swap", typeof(TaskToDoSwap)),
                QueueType.Create("GetBalance", typeof(TaskToDoGetBalance)),
                QueueType.Create("DepositWithdraw", typeof(TaskToDoDepositWithdraw)),
                QueueType.Create("Exchange", typeof(TaskToDoSendAsset)),
                QueueType.Create("GenerateFeeOutputs", typeof(GenerateFeeOutputsResponse)),
                QueueType.Create("GenerateIssuerOutputs", typeof(GenerateIssuerOutputsResponse)),
                QueueType.Create("Transfer", typeof(TaskToDoTransfer)),
                QueueType.Create("GetIssuersOutputStatus", typeof(TaskToDoGetIssuersOutputStatus)),
                QueueType.Create("GetFeeOutputsStatus", typeof(TaskToDoGetFeeOutputsStatus)),
                QueueType.Create("GenerateRefundingTransaction", typeof(TaskToDoGenerateRefundingTransaction)),
                QueueType.Create("GetInputWalletAddresses", typeof(TaskToDoGetInputWalletAddresses)),
                QueueType.Create("UpdateAssets", typeof(TaskToDoUpdateAssets)),
                QueueType.Create("GetExpiredUnclaimedRefundingTransactions", typeof(TaskToDoGetExpiredUnclaimedRefundingTransactions))
                );

            InitializeBitcoinNetwork(Settings);
        }

        [OneTimeTearDown]
        public void TestFixtureTeardown()
        {
            TearDown(Settings);
        }

        public static Settings ReadAppSettings()
        {
            Settings settings = new Settings();
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            settings.AzureStorageEmulatorPath = config.AppSettings.Settings["AzureStorageEmulatorPath"].Value;
            settings.BitcoinDaemonPath = config.AppSettings.Settings["BitcoinDaemonPath"].Value;
            settings.BitcoinWorkingPath = config.AppSettings.Settings["BitcoinWorkingPath"].Value;
            settings.RegtestRPCUsername = config.AppSettings.Settings["RegtestRPCUsername"].Value;
            settings.RegtestRPCPassword = config.AppSettings.Settings["RegtestRPCPassword"].Value;
            settings.RegtestRPCIP = config.AppSettings.Settings["RegtestRPCIP"].Value;
            settings.RegtestPort = int.Parse(config.AppSettings.Settings["RegtestPort"].Value);
            settings.QBitNinjaListenerConsolePath = config.AppSettings.Settings["QBitNinjaListenerConsolePath"].Value;
            settings.WalletBackendExecutablePath = config.AppSettings.Settings["WalletBackendExecutablePath"].Value;
            settings.InQueueConnectionString = config.AppSettings.Settings["InQueueConnectionString"].Value;
            settings.OutQueueConnectionString = config.AppSettings.Settings["OutQueueConnectionString"].Value;
            settings.DBConnectionString = config.AppSettings.Settings["DBConnectionString"].Value;
            return settings;
        }

        public static RPCClient GetRPCClient(Settings setting)
        {
            UriBuilder builder = new UriBuilder();
            builder.Host = setting.RegtestRPCIP;
            builder.Scheme = "http";
            builder.Port = setting.RegtestPort;
            var uri = builder.Uri;

            return new RPCClient(new System.Net.NetworkCredential(setting.RegtestRPCUsername, setting.RegtestRPCPassword),
                uri);
        }

        public static bool PerformShellCommandAndLeave(string commandName, string commandParams,
            out Process startedProcess, string workingDiectory = null, string waitForString = null)
        {
            bool exitFromMethod = false;

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;
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

            startedProcess = p;

            if (!string.IsNullOrEmpty(waitForString))
            {
                p.BeginErrorReadLine();
                p.BeginOutputReadLine();
                var counter = 0;
                while (!exitFromMethod)
                {
                    Thread.Sleep(1000);
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

        public static bool ClearAzureTables(string emulatorPath)
        {
            var commandName = emulatorPath + "AzureStorageEmulator";
            var commandParams = "clear table";

            return PerformShellCommandAndExit(commandName, commandParams);
        }

        public static bool EmptyBitcoinDirectiry(Settings settings)
        {
            var dirPath = settings.BitcoinWorkingPath + "\\data";
            try
            {
                if (Directory.Exists(dirPath))
                {
                    Directory.Delete(dirPath, true);
                }
                Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public static bool StartClearVersionOfBitcoinRegtest(Settings settings)
        {
            if (!EmptyBitcoinDirectiry(settings))
            {
                return false;
            }

            var bitcoinPath = settings.BitcoinDaemonPath + "\\bitcoind.exe";

            Process bitcoinProcess = null;
            PerformShellCommandAndLeave(bitcoinPath, GetBitcoinConfParam(settings), out bitcoinProcess);

            int count = 0;
            var rpcClient = GetRPCClient(settings);
            while (true)
            {
                try
                {
                    rpcClient.GetBlockCount();
                    return true;
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                    count++;
                    if (count > 30)
                    {
                        return false;
                    }
                }
            }
        }

        public static async Task<bool> GenerateBlocks(Settings settings, int count)
        {
            var rpcClient = GetRPCClient(settings);
            RPCResponse response = await rpcClient.SendCommandAsync("generate", new object[] { count });
            if (response.Error != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task<string> SendBTC(Settings settings, BitcoinAddress destination, float amount)
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
            return response.ToString();
        }

        private static string GetBitcoinCliExecPath(Settings settings)
        {
            return settings.BitcoinDaemonPath + "\\bitcoin-cli.exe";
        }

        private static string GetBitcoinConfParam(Settings settings)
        {
            return String.Format("-conf=\"{0}\"", settings.BitcoinWorkingPath + "\\bitcoin.conf");
        }

        private static bool StopBitcoinServer(Settings settings)
        {
            string commandName = GetBitcoinCliExecPath(settings);
            string commandParams = GetBitcoinConfParam(settings) + " stop";
            return PerformShellCommandAndExit(commandName, commandParams);
        }

        private static Process QBitNinjaProcess = null;
        private static Process WalletBackendProcess = null;

        private static bool StartQBitNinjaListener(Settings settings)
        {
            var command = settings.QBitNinjaListenerConsolePath + "\\QBitNinja.Listener.Console.exe";
            var commandParams = "--Listen";
            return PerformShellCommandAndLeave(command, commandParams, out QBitNinjaProcess);
        }

        private static bool StartWalletBackend(Settings settings)
        {
            var command = settings.WalletBackendExecutablePath + "\\ServiceLykkeWallet.exe";
            return PerformShellCommandAndLeave(command, null, out WalletBackendProcess,
                settings.WalletBackendExecutablePath, "Queue reader is started");
        }

        private static void Startup(Settings settings)
        {

            var success = ClearAzureTables(settings.AzureStorageEmulatorPath);
            if (success)
            {
                success = StartClearVersionOfBitcoinRegtest(settings);
                if (success)
                {
                    success = StartQBitNinjaListener(settings);
                    if (success)
                    {
                        success = StartWalletBackend(settings);
                    }
                }
            }

            if (!success)
            {
                throw new Exception("Startup was not successful.");
            }
        }

        private static void FinishQBitNinjaListener()
        {
            QBitNinjaProcess.Kill();
            WalletBackendProcess.Kill();
        }

        private static void TearDown(Settings settings)
        {
            FinishQBitNinjaListener();
            bool success = StopBitcoinServer(settings);
            if (!success)
            {
                throw new Exception("Teardown was not successful.");
            }
        }

        public static T CreateLykkeWalletRequestAndProcessResult<T>(string opName, object param, AzureQueueExt QueueReader, AzureQueueExt QueueWriter) where T : TransactionResponseBase
        {
            var requestString = string.Format("{0}:{1}", opName, Newtonsoft.Json.JsonConvert.SerializeObject(param));
            QueueWriter.PutMessageAsync(requestString).Wait();
            var reply = GetReturnReply<T>(QueueReader);
            if (reply == null)
            {
                throw new Exception(string.Format("{0} reply is null.", opName));
            }
            if (reply.Error != null)
            {
                throw new Exception(string.Format("{0}: {1}", opName, reply.Error.Code.ToString() + ": " + reply.Error.Message));
            }

            return reply;
        }

        public static T GetReturnReply<T>(AzureQueueExt QueueReader)
        {
            object message = null;
            int count = 0;
            while (true)
            {
                message = QueueReader.GetMessage();
                if (message != null)
                {
                    return (T)message;
                }
                count++;
                Thread.Sleep(1000);
                if (count > 30)
                {
                    return default(T);
                }
            }
        }
    }
}
