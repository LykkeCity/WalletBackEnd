using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit;
using NUnit.Framework;
using AzureStorage;
using Common;
using LykkeWalletServices;
using System.Diagnostics;
using Core;
using System.IO;
using System.Net.Http;
using static LykkeWalletServices.OpenAssetsHelper;

namespace Lykkex.WalletBackend.Tests
{
    [SetUpFixture]
    public class SetupFixture :TaskTestsCommon
    {
        private static Process QBitNinjaProcess = null;
        private static Process WalletBackendProcess = null;

        [OneTimeSetUp]
        public async Task TestFixtureSetup()
        {
            Settings = ReadAppSettings();

            ClearDB(Settings);

            await Startup(Settings);

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
                QueueType.Create("TransferAllAssetsToAddress", typeof(TaskToDoTransferAllAssetsToAddress)),
                QueueType.Create("GetIssuersOutputStatus", typeof(TaskToDoGetIssuersOutputStatus)),
                QueueType.Create("GetFeeOutputsStatus", typeof(TaskToDoGetFeeOutputsStatus)),
                QueueType.Create("GenerateRefundingTransaction", typeof(TaskToDoGenerateRefundingTransaction)),
                QueueType.Create("GetInputWalletAddresses", typeof(TaskToDoGetInputWalletAddresses)),
                QueueType.Create("UpdateAssets", typeof(TaskToDoUpdateAssets)),
                QueueType.Create("GetExpiredUnclaimedRefundingTransactions", typeof(TaskToDoGetExpiredUnclaimedRefundingTransactions))
                );

            await InitializeBitcoinNetwork(Settings);

            OpenAssetsHelper.QBitNinjaBaseUrl = Settings.QBitNinjaBaseUrl;
        }

        private async static Task Startup(Settings settings)
        {

            var success = ClearAzureTables(settings.AzureStorageEmulatorPath);
            if (success)
            {
                success = await StartClearVersionOfBitcoinRegtest(settings);
                if (success)
                {
                    success = await StartQBitNinjaListener(settings);
                    if (success)
                    {
                        success = await StartWalletBackend(settings);
                    }
                }
            }

            if (!success)
            {
                throw new Exception("Startup was not successful.");
            }
        }

        private async static Task<bool> StartQBitNinjaListener(Settings settings)
        {
            var command = settings.QBitNinjaListenerConsolePath + "\\QBitNinja.Listener.Console.exe";
            var commandParams = "--Listen";
            return await PerformShellCommandAndLeave(command, commandParams, (p) => QBitNinjaProcess = p);
        }

        private async static Task<bool> StartWalletBackend(Settings settings)
        {
            var command = settings.WalletBackendExecutablePath + "\\ServiceLykkeWallet.exe";
            return await PerformShellCommandAndLeave(command, null, (p) => WalletBackendProcess = p,
            settings.WalletBackendExecutablePath, null);
        }

        public async static Task<bool> StartClearVersionOfBitcoinRegtest(Settings settings)
        {
            if (!EmptyBitcoinDirectiry(settings))
            {
                return false;
            }

            var bitcoinPath = settings.BitcoinDaemonPath + "\\bitcoind.exe";

            Process bitcoinProcess = null;
            await PerformShellCommandAndLeave(bitcoinPath, GetBitcoinConfParam(settings),
                 (p) => bitcoinProcess = p);

            int count = 0;
            var rpcClient = GetRPCClient(settings);
            while (true)
            {
                try
                {
                    await rpcClient.GetBlockCountAsync();
                    return true;
                }
                catch (Exception e)
                {
                    await Task.Delay(1000);
                    count++;
                    if (count > 30)
                    {
                        return false;
                    }
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

        [OneTimeTearDown]
        public void TestFixtureTeardown()
        {
            TearDown(Settings);
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

        private static void FinishQBitNinjaListener()
        {
            QBitNinjaProcess.Kill();
            WalletBackendProcess.Kill();
        }

        private static bool StopBitcoinServer(Settings settings)
        {
            string commandName = GetBitcoinCliExecPath(settings);
            string commandParams = GetBitcoinConfParam(settings) + " stop";
            return PerformShellCommandAndExit(commandName, commandParams);
        }

        private static string GetBitcoinCliExecPath(Settings settings)
        {
            return settings.BitcoinDaemonPath + "\\bitcoin-cli.exe";
        }

        private static string GetBitcoinConfParam(Settings settings)
        {
            return String.Format("-conf=\"{0}\"", settings.BitcoinWorkingPath + "\\bitcoin.conf");
        }
    }
}
