using LykkeWalletServices;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace ServiceLykkeWallet.Controllers
{
    public class OffchainController : ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> AddClientProvidedChannelInput(string multiSig, string transactionId)
        {
            return Ok();
        }

        public class UnsignedChannelSetupTransaction
        {
            public string UnsigndTransaction
            {
                get;
                set;
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> GenerateUnsignedChannelSetupTransaction(string clientPubkey, double clientContributedAmount,
            string hubPubkey, double hubContributedAmount, string channelAssetName, int channelTimeoutInMinutes)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                var multisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);
                var asset = OpenAssetsHelper.GetAssetFromName(WebSettings.Assets, channelAssetName,
                    WebSettings.ConnectionParams.BitcoinNetwork);
                if (asset == null && channelAssetName.ToLower() != "btc")
                {
                    return InternalServerError(new Exception(string.Format("The specified asset is not supported {0}.",
                        channelAssetName)));
                }

                var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(multisig.MultiSigAddress, WebSettings.ConnectionParams.BitcoinNetwork,
                    entities);
                if (walletOutputs.Item2 && !(walletOutputs?.Item3 ?? string.Empty).ToLower().StartsWith("no coins "))
                {
                    return InternalServerError(new Exception(string.Format("Error in getting outputs for wallet: {0}, the error is {1}",
                        multisig.MultiSigAddress, walletOutputs.Item3)));
                }
                else
                {
                    if (walletOutputs.Item4)
                    {
                        // We should not reach probably by using LykkkeBlockchainManager
                        // ToDo: Clarification required
                        return InternalServerError(new Exception(string.Format("Some inputs are in race with a refund for address: {0}",
                            multisig.MultiSigAddress)));
                    }
                    else
                    {
                        var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, asset.AssetId);
                        var coins = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, asset.AssetId);
                        long totalAmount = 0;
                        if (asset == null)
                        {
                            totalAmount = coins.Item2.Sum(c => c.Amount);
                        }
                        else
                        {
                            totalAmount = coins.Item1.Sum(c => c.Amount.Quantity);
                        }

                        return await GenerateUnsignedChannelSetupTransactionCore(clientPubkey, clientContributedAmount, hubPubkey,
                            hubContributedAmount, totalAmount, channelAssetName, channelTimeoutInMinutes);
                    }
                }
            }
        }

        // http://localhost:8989/Offchain/GenerateUnsignedChannelSetupTransaction?ClientAddress=x&ClientContributedAmount=10&HubAddress=z&HubContributedAmount=10&ClientMultisigAddress=ab&ClientMultisigContributedAmount=10&ChannelAssetName=ac&channelTimeoutInMinutes=5
        [HttpGet]
        public async Task<IHttpActionResult> GenerateUnsignedChannelSetupTransactionCore(string clientPubkey, double clientContributedAmount,
            string hubPubkey, double hubContributedAmount, double multisigNewlyAddedAmount, string channelAssetName,
            int channelTimeoutInMinutes)
        {
            try
            {
                string txHex = null;
                var btcAsset = (channelAssetName.ToLower() == "btc");
                var clientAddress = (new PubKey(clientPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                var hubAddress = (new PubKey(hubPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork);
                var multisig = GetMultiSigFromTwoPubKeys(clientPubkey, hubPubkey);

                var asset = WebSettings.Assets.Where(a => a.Name == channelAssetName).FirstOrDefault();
                OpenAssetsHelper.GetAssetFromName(WebSettings.Assets, channelAssetName, WebSettings.ConnectionParams.BitcoinNetwork);
                if (asset == null && channelAssetName.ToLower() != "btc")
                {
                    return InternalServerError(new Exception(string.Format("The specified asset is not supported {0}.", channelAssetName)));
                }
                var assetId = asset?.AssetId;
                var assetMultiplyFactor = asset?.MultiplyFactor ?? (long)OpenAssetsHelper.BTCToSathoshiMultiplicationFactor;

                long[] contributedAmount = new long[3];
                long[] requiredAssetAmount = new long[3];
                requiredAssetAmount[0] = (long)(clientContributedAmount * assetMultiplyFactor);
                requiredAssetAmount[1] = (long)(multisigNewlyAddedAmount * asset.MultiplyFactor);
                requiredAssetAmount[2] = (long)(hubContributedAmount * assetMultiplyFactor);

                IList<ICoin>[] coinToBeUsed = new IList<ICoin>[3];
                BitcoinAddress[] inputAddress = new BitcoinAddress[3];
                double[] inputAmount = new double[3];

                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
                {
                    /*
                    if (string.IsNullOrEmpty(ClientMultisigAddress))
                    {
                        var multisig = await OpenAssetsHelper
                            .GetMatchingMultisigAddress(ClientMultisigAddress, entities);

                        var parameters = PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters
                            (new Script(multisig.MultiSigScript));
                        if (!string.IsNullOrEmpty(ClientAddress) && !DoesPubKeyMatchesAddress(parameters.PubKeys[0], ClientAddress))
                        {
                            return InternalServerError(new Exception
                                (string.Format(multsigMismatchAddressError, "client")));
                        }
                        if (!string.IsNullOrEmpty(HubAddress) && !DoesPubKeyMatchesAddress(parameters.PubKeys[1], ClientAddress))
                        {
                            return InternalServerError(new Exception
                                (string.Format(multsigMismatchAddressError, "Hub")));
                        }
                    }
                    */

                    for (int i = 0; i < 3; i++)
                    {
                        coinToBeUsed[i] = new List<ICoin>();
                        switch (i)
                        {
                            case 0:
                                inputAddress[i] = clientAddress;
                                inputAmount[i] = clientContributedAmount;
                                break;
                            case 1:
                                inputAddress[i] = Base58Data.GetFromBase58Data(multisig.MultiSigAddress) as BitcoinScriptAddress;
                                inputAmount[i] = multisigNewlyAddedAmount;
                                break;
                            case 2:
                                inputAddress[i] = hubAddress;
                                inputAmount[i] = hubContributedAmount;
                                break;
                        }

                        var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(inputAddress[i].ToString(), WebSettings.ConnectionParams.BitcoinNetwork,
                            entities);
                        if (walletOutputs.Item2 && !(walletOutputs?.Item3 ?? string.Empty).ToLower().StartsWith("no coins "))
                        {
                            return InternalServerError(new Exception(string.Format("Error in getting outputs for wallet: {0}, the error is {1}",
                                inputAddress, walletOutputs.Item3)));
                        }
                        else
                        {
                            if (walletOutputs.Item4)
                            {
                                // We should not reach probably by using LykkkeBlockchainManager
                                // ToDo: Clarification required
                                return InternalServerError(new Exception(string.Format("Some inputs are in race with a refund for address: {0}",
                                    inputAddress)));
                            }

                            var assetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(walletOutputs.Item1, assetId);
                            ICoin[] coinToSelectFrom;
                            if (btcAsset)
                            {
                                coinToSelectFrom = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, assetId).Item2;
                            }
                            else
                            {
                                coinToSelectFrom = OpenAssetsHelper.GetColoredUnColoredCoins(assetOutputs, assetId).Item1;
                            }

                            contributedAmount[i] = 0;

                            if (requiredAssetAmount[i] > 0)
                            {
                                foreach (var item in coinToSelectFrom)
                                {
                                    if (btcAsset)
                                    {
                                        contributedAmount[i] += ((Coin)item).Amount;
                                    }
                                    else
                                    {
                                        contributedAmount[i] += ((ColoredCoin)item).Amount.Quantity;
                                    }

                                    if (i == 1)
                                    {
                                        if (btcAsset)
                                        {
                                            var bearer = ((ColoredCoin)item).Bearer;
                                            var scriptBearer =
                                                new ScriptCoin(bearer, new Script(multisig.MultiSigScript));
                                            var coloredScriptCoin = new ColoredCoin(((ColoredCoin)item).Amount, scriptBearer);
                                            coinToBeUsed[i].Add(coloredScriptCoin);
                                        }
                                        else
                                        {
                                            var scriptItem =
                                                new ScriptCoin((Coin)item, new Script(multisig.MultiSigScript));
                                            coinToBeUsed[i].Add(item);
                                        }
                                    }
                                    else
                                    {
                                        coinToBeUsed[i].Add(item);
                                    }

                                    if (contributedAmount[i] >= requiredAssetAmount[i])
                                    {
                                        break;
                                    }
                                }
                            }

                            if (contributedAmount[i] < requiredAssetAmount[i])
                            {
                                return InternalServerError(new Exception(string.Format("Address {0} has not {1} of {2}.",
                                    inputAddress[i], inputAmount[i], channelAssetName)));
                            }
                        }
                    } // end of for

                    TransactionBuilder builder = new TransactionBuilder();
                    for (int i = 0; i < 3; i++)
                    {
                        builder.AddCoins(coinToBeUsed[i]);
                    }

                    var directSendValue = 0L;
                    var returnValue = 0L;

                    var numberOfColoredCoinOutputs = 0;

                    for (int i = 0; i < 3; i++)
                    {
                        directSendValue = requiredAssetAmount[i];
                        returnValue = contributedAmount[i] - requiredAssetAmount[i];

                        if (directSendValue <= 0)
                        {
                            continue;
                        }

                        var multisigAddress = Base58Data.GetFromBase58Data(multisig.MultiSigAddress) as BitcoinAddress;
                        if (btcAsset)
                        {
                            builder.Send(multisigAddress, new Money(directSendValue));
                            if (returnValue > 0)
                            {
                                builder.Send(inputAddress[i], new Money(returnValue));
                            }
                        }
                        else
                        {
                            builder.SendAsset(multisigAddress,
                                new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), directSendValue));
                            numberOfColoredCoinOutputs++;

                            if (returnValue > 0)
                            {
                                builder.SendAsset(inputAddress[i], new AssetMoney(new AssetId(new BitcoinAssetId(assetId)),
                                    returnValue));
                                numberOfColoredCoinOutputs++;
                            }
                        }
                    }

                    using (var transaction = entities.Database.BeginTransaction())
                    {
                        await builder.AddEnoughPaymentFee(entities, WebSettings.ConnectionParams, WebSettings.FeeAddress,
                            numberOfColoredCoinOutputs, -1, "Offchain" + multisig.MultiSigAddress, Guid.NewGuid().ToString());
                        txHex = builder.BuildTransaction(false).ToHex();
                        var txHash = Convert.ToString(SHA256Managed.Create().ComputeHash(OpenAssetsHelper.StringToByteArray(txHex)));
                        var channel = entities.OffchainChannels.Add(new OffchainChannel { unsignedTransactionHash = txHash });
                        await entities.SaveChangesAsync();

                        for (int i = 0; i < 3; i++)
                        {
                            string toBeStoredTxId = null;
                            int toBeStoredTxOutputNumber = 0;

                            foreach (var item in coinToBeUsed[i])
                            {
                                if (btcAsset)
                                {
                                    toBeStoredTxId = ((Coin)item).Outpoint.Hash.ToString();
                                    toBeStoredTxOutputNumber = (int)((Coin)item).Outpoint.N;
                                }
                                else
                                {
                                    toBeStoredTxId = ((ColoredCoin)item).Bearer.Outpoint.Hash.ToString();
                                    toBeStoredTxOutputNumber = (int)((ColoredCoin)item).Bearer.Outpoint.N;
                                }

                                var now = DateTime.UtcNow;
                                var coin = new ChannelCoin
                                {
                                    OffchainChannel = channel,
                                    TransactionId = toBeStoredTxId,
                                    OutputNumber = toBeStoredTxOutputNumber,
                                    ReservationCreationDate = now,
                                    ReservedForChannel = channel.ChannelId,
                                    ReservedForMultisig = multisig.MultiSigAddress,
                                    ReservationEndDate = now.AddMinutes(channelTimeoutInMinutes)
                                };
                                entities.ChannelCoins.Add(coin);
                            }
                        }

                        await entities.SaveChangesAsync();
                        transaction.Commit();
                    }
                }
                return Json(new UnsignedChannelSetupTransaction { UnsigndTransaction = txHex });
            }
            catch (Exception e)
            {
                return InternalServerError(e);
            }
        }

        private static KeyStorage GetMultiSigFromTwoPubKeys(string clientPubkey, string hubPubkey)
        {
            var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { new PubKey(clientPubkey) ,
                (new PubKey(hubPubkey)) });
            var multiSigAddressFormat = multiSigAddress.GetScriptAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();

            var retValue = new KeyStorage();
            retValue.ExchangePrivateKey = null;
            retValue.WalletPrivateKey = null;
            retValue.MultiSigAddress = multiSigAddressFormat;
            retValue.MultiSigScript = multiSigAddress.ToString();
            retValue.WalletAddress = (new PubKey(clientPubkey)).GetAddress(WebSettings.ConnectionParams.BitcoinNetwork)
                .ToString();
            return retValue;
        }

        private static bool DoesPubKeyMatchesAddress(PubKey pubKey, string address)
        {
            var oneSideAddress = pubKey.GetAddress(WebSettings.ConnectionParams.BitcoinNetwork).ToString();
            if (oneSideAddress != address)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public class UnsignedClientCommitmentTransactionResponse
        {
            public string FullySignedSetupTransaction
            {
                get;
                set;
            }

            public string UnsignedClientCommitment0
            {
                get;
                set;
            }
        }

        // http://localhost:8989/Offchain/CreateUnsignedClientCommitmentTransaction?UnsignedChannelSetupTransaction=0001&ClientSignedChannelSetup=0001
        [HttpGet]
        public async Task<IHttpActionResult> CreateUnsignedClientCommitmentTransaction(string UnsignedChannelSetupTransaction, string ClientSignedChannelSetup
            , double clientCommitedAmount, double hubCommitedAmount)
        {
            return Json(new UnsignedClientCommitmentTransactionResponse
            {
                FullySignedSetupTransaction = "0002",
                UnsignedClientCommitment0 = "0002"
            });
        }

        public class FinalizeChannelSetupResponse
        {
            public string SignedHubCommitment0
            {
                get;
                set;
            }
        }

        // http://localhost:8989/Offchain/FinalizeChannelSetup?FullySignedSetupTransaction=002&SignedClientCommitment0=002
        [HttpGet]
        public async Task<IHttpActionResult> FinalizeChannelSetup(string FullySignedSetupTransaction, string SignedClientCommitment0)
        {
            return Json(new FinalizeChannelSetupResponse { SignedHubCommitment0 = "0003" });
        }

        [HttpGet]
        public async Task<IHttpActionResult> CreateUnsignedCommitmentTransactions(string signedSetupTransaction, string clientPubkey,
            string hubPubkey, double clientAmount, double hubAmount, string lockingPubkey, int selfActivationInMinutes, bool clientSendsCommitmentToHub)
        {
            return Ok();
        }

        [HttpGet]
        public async Task<IHttpActionResult> CheckHalfSignedCommitmentTransactionToBeCorrect(string halfSignedCommitment, string signedSetupTransaction, string clientPubkey,
            string hubPubkey, double clientAmount, double hubAmount, string lockingPubkey, int selfActivationInMinutes, bool clientSendsCommitmentToHub)
        {
            return Ok();
        }

        [HttpGet]
        public async Task<IHttpActionResult> AddEnoughFeesToCommitentAndBroadcast(string commitmentTransaction)
        {
            return Ok();
        }
    }
}
