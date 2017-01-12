using Core;
using LykkeWalletServices;
using NBitcoin;
using NBitcoin.OpenAsset;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;
using System.Collections.Concurrent;

namespace Lykkex.WalletBackend.Tests
{
    [TestFixture]
    public class SwapTests : TaskTestsCommon
    {
        public IList<GenerateNewWalletTaskResult> usdWallets = new List<GenerateNewWalletTaskResult>();
        public IList<GenerateNewWalletTaskResult> eurWallets = new List<GenerateNewWalletTaskResult>();
        public ConcurrentBag<string> swapResult = new ConcurrentBag<string>();
        public ConcurrentBag<string> swapError = new ConcurrentBag<string>();
        public string[] usdWalletGuids = new string[100];
        public string[] eurWalletGuids = new string[100];
        public string[] swapGuids = new string[100];

        public void GenerateNewWalletCallback(string transactionId, TransactionResponseBase result)
        {
            if (usdWalletGuids.Contains(transactionId))
            {
                usdWallets.Add(((GenerateNewWalletResponse)result).Result);
            }

            if (eurWalletGuids.Contains(transactionId))
            {
                eurWallets.Add(((GenerateNewWalletResponse)result).Result);
            }
        }

        private void ClearState()
        {
            usdWallets = new List<GenerateNewWalletTaskResult>();
            eurWallets = new List<GenerateNewWalletTaskResult>();
            swapResult = new ConcurrentBag<string>();
            swapError = new ConcurrentBag<string>();
            usdWalletGuids = new string[100];
            eurWalletGuids = new string[100];
            swapGuids = new string[100];
        }

        public void SwapCallback(string transactionId, TransactionResponseBase result)
        {
            if (swapGuids.Contains(transactionId))
            {
                var resp = ((SwapResponse)result);
                if (resp.Error == null)
                {
                    swapResult.Add(resp.Result.TransactionHash);
                }
                else
                {
                    swapError.Add(string.Format("Error number:{0} ,Error Message: {1}",
                        resp.Error.Code, resp.Error.Message));
                }
            }
        }

        private async Task Perform100SwapsFromClearState()
        {
            ClearState();

            GenerateNewWalletTaskResult masterUsdWallet = await GenerateNewWallet(QueueReader, QueueWriter);
            GenerateNewWalletTaskResult masterEurWallet = await GenerateNewWallet(QueueReader, QueueWriter);

            usdWallets = new List<GenerateNewWalletTaskResult>();
            eurWallets = new List<GenerateNewWalletTaskResult>();

            for (int i = 0; i < 100; i++)
            {
                var usdGuid = Guid.NewGuid();
                var eurGuid = Guid.NewGuid();

                usdWalletGuids[i] = usdGuid.ToString();
                eurWalletGuids[i] = eurGuid.ToString();

                await GenerateNewWallet(QueueReader, QueueWriter, GenerateNewWalletCallback, usdGuid);
                await GenerateNewWallet(QueueReader, QueueWriter, GenerateNewWalletCallback, eurGuid);
            }

            while (usdWallets.Count < 100 && eurWallets.Count < 100)
            {
                Thread.Sleep(300);
            }

            await CashinToAddress(masterUsdWallet.WalletAddress, "TestExchangeUSD", 1000);
            await CashinToAddress(masterEurWallet.WalletAddress, "TestExchangeEUR", 1000);

            var usdAsset = await GetAssetFromName("TestExchangeUSD");
            var eurAsset = await GetAssetFromName("TestExchangeEUR");

            var usdSourceCoin = await GetAssetSourceCoin(masterUsdWallet.WalletAddress, usdAsset.AssetId);
            var eurSourceCoin = await GetAssetSourceCoin(masterEurWallet.WalletAddress, eurAsset.AssetId);

            await DistributeColoredCoins(usdWallets.ToArray(), usdAsset, usdSourceCoin, 10, masterUsdWallet);
            await DistributeColoredCoins(eurWallets.ToArray(), eurAsset, eurSourceCoin, 10, masterEurWallet);
            await GenerateBlocks(Settings, 1);
            for (int i = 0; i < 100; i++)
            {
                swapGuids[i] = Guid.NewGuid().ToString();
                await SwapAssets(swapGuids[i], usdWallets[i].MultiSigAddress, "TestExchangeUSD", 1,
                    eurWallets[i].MultiSigAddress, "TestExchangeEUR", 1, SwapCallback);
            }

            while ((swapError.Count() + swapResult.Count()) < 100)
            {
                Thread.Sleep(300);
            }

            await GenerateBlocks(Settings, 1);
        }

        [Test]
        public async Task SwapFeesShouldBeConsistent()
        {
            await Perform100SwapsFromClearState();

            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                while(true)
                {
                    if(entities.PregeneratedReserves.Count() > 0)
                    {
                        await Task.Delay(10000);
                    }
                    else
                    {
                        break;
                    }
                }

                bool actualSpentStatus;
                foreach(var fee in entities.PreGeneratedOutputs)
                {
                    actualSpentStatus = await PregeneratedHasBeenSpentInBlockchain
                        (fee, WebSettings.ConnectionParams);
                    if(actualSpentStatus != (fee.Consumed == 1 ? true : false))
                    {
                        Assert.Fail(string.Format("Please check fee for TxId:{0} and output number:{1} ",
                            fee.TransactionId, fee.OutputNumber));
                    }
                }
            }
        }

        [Test]
        public async Task SwapFeeStressTestFailuresLess5Percent()
        {
            await Perform100SwapsFromClearState();

            Assert.LessOrEqual(swapError.Count(), 5);
        }

        private async Task<Asset> GetAssetFromName(string assetname)
        {
            string url = string.Format("http://localhost:8989/General/GetAssetFromName?assetname={0}", assetname);
            using (HttpClient webClient = new HttpClient())
            {
                var str = await webClient.GetStringAsync(url);
                dynamic deserialize = JsonConvert.DeserializeObject(str);
                return new Asset
                {
                    AssetId = deserialize.AssetId,
                    AssetMultiplicationFactor = deserialize.AssetMultiplicationFactor
                };
            }
        }

        private static async Task DistributeColoredCoins(GenerateNewWalletTaskResult[] wallets, Asset asset,
            ColoredCoin sourceCoin, int eachOutputAmount, GenerateNewWalletTaskResult sourceWallet)
        {
            var assetMoney = new AssetMoney(new AssetId(new BitcoinAssetId(asset.AssetId)),
                    eachOutputAmount * asset.AssetMultiplicationFactor);
            TransactionBuilder builder = new TransactionBuilder();
            builder.AddKeys(new BitcoinSecret(sourceWallet.WalletPrivateKey));
            builder.AddCoins(sourceCoin);
            for (int i = 0; i < wallets.Count(); i++)
            {
                var usdAddress = Base58Data.GetFromBase58Data(wallets[i].MultiSigAddress) as BitcoinAddress;
                builder.SendAsset(usdAddress, assetMoney);
            }

            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                await builder.AddEnoughPaymentFee(entities, WebSettings.ConnectionParams, WebSettings.FeeAddress, 100);
            }
            var tx = builder.BuildTransaction(true);

            var rpcClient = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential
                            (WebSettings.ConnectionParams.Username, WebSettings.ConnectionParams.Password),
                                    WebSettings.ConnectionParams.IpAddress, WebSettings.ConnectionParams.BitcoinNetwork);
            await rpcClient.SendRawTransactionAsync(tx);

            await WaitUntillQBitNinjaHasIndexed(Settings, HasTransactionIndexed,
                        new string[] { tx.GetHash().ToString() }, null);
        }

        private static async Task<ColoredCoin> GetAssetSourceCoin(string walletAddress, string assetName)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(walletAddress,
                    WebSettings.ConnectionParams.BitcoinNetwork, entities);
                if (walletOutputs.Item2)
                {
                    Assert.Fail(walletOutputs.Item3);
                    return null; // We normally not reach here
                }
                else
                {
                    var coins = OpenAssetsHelper.GetColoredUnColoredCoins
                        (walletOutputs.Item1, assetName, WebSettings.Assets);
                    return (coins.Item1)[0];
                }
            }
        }
    }
}
