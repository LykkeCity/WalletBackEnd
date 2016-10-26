using Common;
using Common.Log;
using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Core.LykkeIntegration.Services;
using static LykkeWalletServices.Transactions.TaskHandlers.SettingsReader;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using LykkeWalletServices.TimerServices;
using Newtonsoft.Json.Linq;

namespace LykkeWalletServices
{
    public static class OpenAssetsHelper
    {
        public class RPCConnectionParams
        {
            public string Username
            {
                get;
                set;
            }

            public string Password
            {
                get;
                set;
            }

            public string IpAddress
            {
                get;
                set;
            }

            public string Network
            {
                get;
                set;
            }

            public Network BitcoinNetwork
            {
                get
                {
                    switch (Network.ToLower())
                    {
                        case "main":
                            return NBitcoin.Network.Main;
                        case "testnet":
                            return NBitcoin.Network.TestNet;
                        default:
                            throw new NotImplementedException(string.Format("Bitcoin network {0} is not supported.", Network));
                    }
                }
            }
        }
        public enum APIProvider
        {
            QBitNinja
        }

        public const uint MinimumRequiredSatoshi = 50000; // 100000000 satoshi is one BTC
        public static uint TransactionSendFeesInSatoshi
        {
            get;
            set;
        }

        public const uint DefaultTransactionSendFeesInSatoshi = 20000;
        public const int MinimumTransactionSendFeesInSatoshi = 10000;
        public const int MaximumTransactionSendFeesInSatoshi = 100000;
        public const ulong BTCToSathoshiMultiplicationFactor = 100000000;
        public const uint ConcurrencyRetryCount = 3;
        public const uint NBitcoinColoredCoinOutputInSatoshi = 2730;
        public const APIProvider apiProvider = APIProvider.QBitNinja;
        public const int LocktimeMinutesAllowance = 120;

        public static ILog GeneralLogger
        {
            get;
            set;
        }

        public static string QBitNinjaBalanceUrl
        {
            get
            {
                return QBitNinjaBaseUrl + "balances/";
            }
        }

        public static string QBitNinjaTransactionUrl
        {
            get
            {
                return QBitNinjaBaseUrl + "transactions/";
            }
        }

        public static string QBitNinjaBaseUrl
        {
            get;
            set;
        }

        public static int PreGeneratedOutputMinimumCount
        {
            get;
            set;
        }

        public static int BroadcastGroup
        {
            get;
            set;
        }

        public static uint FeeMultiplicationFactor
        {
            get;
            set;
        }

        public static FeeType21co FeeType
        {
            get;
            set;
        }

        public static string EnvironmentName
        {
            get;
            set;
        }

        public static bool PrivateKeyWillBeSubmitted
        {
            get;
            set;
        }

        public static IQueueExt EmailQueueWriter
        {
            get;
            set;
        }

        private static int minimumNumberOfRequiredConfirmations = 1;

        public static int MinimumNumberOfRequiredConfirmations
        {
            get
            {
                return minimumNumberOfRequiredConfirmations;
            }

            set
            {
                minimumNumberOfRequiredConfirmations = value;
            }
        }

        private readonly static Func<int> DefaultGetMinimumConfirmationNumber = (() => { return MinimumNumberOfRequiredConfirmations; });

        public static IDictionary<string, string> P2PKHDictionary = new Dictionary<string, string>();
        public static IDictionary<string, string> MultisigDictionary = new Dictionary<string, string>();
        public static IDictionary<string, string> MultisigScriptDictionary = new Dictionary<string, string>();

        public static Network Network
        {
            get;
            set;
        }

        #region UniversalUnspentPropertyProxyFunctions
        public static long GetValue(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).value;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static string GetTransactionHash(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).transaction_hash;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static string GetScriptHex(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).script_hex;
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static int GetOutputIndex(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).output_index;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static string GetAssetId(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).asset_id;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static int GetConfirmationNumber(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).confirmations;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static long GetAssetAmount(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).asset_quantity;
                default:
                    throw new Exception("Not supported.");
            }
        }

        private static long GetBitcoinAmount(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).value;
                default:
                    throw new Exception("Not supported.");
            }
        }
        #endregion

        public static Coin GetCoin(this PreGeneratedOutput output)
        {
            Network network = null;
            switch (output.Network.ToLower())
            {
                case "main":
                    network = Network.Main;
                    break;
                case "testnet":
                    network = Network.TestNet;
                    break;
                default:
                    // We should never reach here
                    throw new Exception("Not a valid network");
            }
            return new Coin(new uint256(output.TransactionId), (uint)output.OutputNumber,
                                    output.Amount, new PayToPubkeyHashTemplate().GenerateScriptPubKey(new BitcoinPubKeyAddress(output.Address, network)));
        }

        public static TransactionBuilder SendAssetWithChange(this TransactionBuilder builder, IDestination destination,
            AssetMoney assetMoney, ColoredCoin[] coins, IDestination changeDestination)
        {
            long sum = coins.Sum(c => c.Amount.Quantity);
            builder.SendAsset(destination, assetMoney);

            if (sum - assetMoney.Quantity > 0)
            {
                var changeAssetMoney = new AssetMoney(assetMoney.Id, sum - assetMoney.Quantity);
                builder.SendAsset(changeDestination, changeAssetMoney);
            }

            return builder;
        }

        public static TransactionBuilder SendWithChange(this TransactionBuilder builder, IDestination destination,
            Money amount, Coin[] coins, IDestination changeDestination)
        {
            long sum = coins.Sum(c => c.Amount);
            builder.Send(destination, amount);
            if (sum - amount > 0)
            {
                builder.Send(changeDestination, sum - amount);
            }
            return builder;
        }

        // From transaction 0cb81047ab5945a59d578ca015804bc539d3af78523d90287335de57fdfe7fd8
        // Following is what a signed input will add to a transaction (143 bytes)
        // a172379843146d35b29fc4524a6ef9f9cba2e6dbe8956e3e76c1619cdda35e0b
        // 6b000000
        // 6a
        // 47304402202ae6c3693442d090285ae0b37ed7020e7490af4a3cce20f6e83d0918e8e5e04f02205248928b5eac2df48d13d72bc70e91152deec38efe80f044900f3d4e83367db6012103958ff7db47a01390f4a0ba0266a1750f00468c2d1d749ef872aa2b2b34d1a6ef
        // ffffffff

        public static async Task<TransactionBuilder> AddEnoughPaymentFee(this TransactionBuilder builder, SqlexpressLykkeEntities entities,
            RPCConnectionParams connectionParams, string feeAddress, long requiredNumberOfColoredCoinFee = 1, long estimatedFee = -1,
            string reservedForAddress = null, string reserveId = null)
        {
            builder.SetChange(BitcoinAddress.Create(feeAddress), ChangeType.Uncolored);

            long totalAddedFee = 0;

            var feeRate = await GetFeeRate();
            var feeConsumptionItself = (long)((143.0 / 1024) * feeRate.FeePerK.Satoshi);

            if (estimatedFee == -1)
            {
                Transaction tx = null;
                bool continueLoop = true;
                int counter = 0;
                while (continueLoop)
                {
                    try
                    {
                        tx = builder.BuildTransaction(false);
                        continueLoop = false;
                    }
                    catch (NotEnoughFundsException ex)
                    {
                        PreGeneratedOutput feePayer = await GetOnePreGeneratedOutput(entities, connectionParams, 2 * feeConsumptionItself, null,
                            reservedForAddress, reserveId);
                        Coin feePayerCoin = feePayer.GetCoin();

                        totalAddedFee += feePayer.Amount;
                        builder.AddKeys(new BitcoinSecret(feePayer.PrivateKey)).AddCoins(feePayerCoin);

                        counter++;
                    }
                }

                estimatedFee = builder.EstimateFees(tx, feeRate);
            }

            var requiredFee = estimatedFee + requiredNumberOfColoredCoinFee * NBitcoinColoredCoinOutputInSatoshi;


            while (true)
            {
                if (totalAddedFee >= requiredFee)
                {
                    break;
                }

                PreGeneratedOutput feePayer = await GetOnePreGeneratedOutput(entities, connectionParams, 2 * feeConsumptionItself, reservedForAddress, reserveId);
                Coin feePayerCoin = feePayer.GetCoin();

                totalAddedFee += feePayer.Amount;
                builder.AddKeys(new BitcoinSecret(feePayer.PrivateKey)).AddCoins(feePayerCoin);
                requiredFee += feeConsumptionItself;
            }
            builder
                .SendFees(new Money(totalAddedFee - requiredNumberOfColoredCoinFee * NBitcoinColoredCoinOutputInSatoshi)); // We put all added fee here, since estimate is under estimate

            return builder;
        }

        internal static Coin GetCoinFromOutput(this UniversalUnspentOutput output)
        {
            return new Coin(new uint256(output.GetTransactionHash()), (uint)output.GetOutputIndex(),
                    new Money(output.GetValue()), new Script(StringToByteArray(output.GetScriptHex())));
        }

        internal static ColoredCoin[] GenerateWalletColoredCoins(UniversalUnspentOutput[] usableOutputs, string assetId)
        {
            ColoredCoin[] coins = new ColoredCoin[usableOutputs.Length];
            for (int i = 0; i < usableOutputs.Length; i++)
            {
                Coin bearer = usableOutputs[i].GetCoinFromOutput();
                coins[i] = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), usableOutputs[i].GetAssetAmount()),
                    bearer);
            }
            return coins;
        }

        private static Coin[] GenerateWalletUnColoredCoins(UniversalUnspentOutput[] usableOutputs)
        {
            Coin[] coins = new Coin[usableOutputs.Length];
            for (int i = 0; i < usableOutputs.Length; i++)
            {
                coins[i] = usableOutputs[i].GetCoinFromOutput();
            }
            return coins;
        }

        /*
        private static ColoredCoin[] GenerateWalletColoredCoins(Transaction[] transactions, UniversalUnspentOutput[] usableOutputs, string assetId)
        {
            ColoredCoin[] coins = new ColoredCoin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), (int)usableOutputs[i].GetAssetAmount()),
                    new Coin(transactions[i], (uint)usableOutputs[i].GetOutputIndex()));
            }
            return coins;
        }

        private static Coin[] GenerateWalletUnColoredCoins(Transaction[] transactions, UniversalUnspentOutput[] usableOutputs)
        {
            Coin[] coins = new Coin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new Coin(transactions[i], (uint)usableOutputs[i].GetOutputIndex());
            }
            return coins;
        }
        */

        public static UniversalUnspentOutput[] GetWalletOutputsUncolored(UniversalUnspentOutput[] input)
        {
            IList<UniversalUnspentOutput> outputs = new List<UniversalUnspentOutput>();
            foreach (var item in input)
            {
                if (item.GetAssetId() == null)
                {
                    outputs.Add(item);
                }
            }

            return outputs.ToArray();
        }

        public static UniversalUnspentOutput[] GetWalletOutputsForAsset(UniversalUnspentOutput[] input,
            string assetId)
        {
            IList<UniversalUnspentOutput> outputs = new List<UniversalUnspentOutput>();
            if (assetId != null)
            {
                foreach (var item in input)
                {
                    if (item.GetAssetId() == assetId)
                    {
                        outputs.Add(item);
                    }
                }
            }

            return outputs.ToArray();
        }

        public static Tuple<ColoredCoin[], Coin[]> GetColoredUnColoredCoins(UniversalUnspentOutput[] walletOutputs,
            string assetId)
        {
            var walletAssetOutputs = GetWalletOutputsForAsset(walletOutputs, assetId);
            var walletUncoloredOutputs = GetWalletOutputsUncolored(walletOutputs);
            var walletColoredCoins = GenerateWalletColoredCoins(walletAssetOutputs, assetId);
            var walletUncoloredCoins = GenerateWalletUnColoredCoins(walletUncoloredOutputs);
            return new Tuple<ColoredCoin[], Coin[]>(walletColoredCoins, walletUncoloredCoins);
        }

        public static async Task<Tuple<UniversalUnspentOutput[], bool, string>> GetWalletOutputs(string walletAddress,
            Network network, SqlexpressLykkeEntities entities, Func<int> getMinimumConfirmationNumber = null, bool ignoreUnconfirmed = false)
        {
            Tuple<UniversalUnspentOutput[], bool, string> ret = null;
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    var qbitResult = await GetWalletOutputsQBitNinja(walletAddress, network, entities, getMinimumConfirmationNumber, ignoreUnconfirmed);
                    ret = new Tuple<UniversalUnspentOutput[], bool, string>(qbitResult.Item1 != null ? qbitResult.Item1.Select(c => (UniversalUnspentOutput)c).ToArray() : null,
                        qbitResult.Item2, qbitResult.Item3);
                    break;
                default:
                    throw new Exception("Not supported.");
            }

            /*
            IList<UniversalUnspentOutput> retList = new List<UniversalUnspentOutput>();
            var joined = from output in ret.Item1
                         join refunded in entities.RefundedOutputs
                         on new { A = output.GetTransactionHash(), B = output.GetOutputIndex() } equals new { A = refunded.RefundedTxId, B = refunded.RefundedOutputNumber }
                         into gj
                         from item in gj.DefaultIfEmpty(new RefundedOutput { LockTime = DateTime.MaxValue })
                         where item.HasBeenSpent.Equals(false) && item.RefundInvalid.Equals(false)
                         select new { output, item.LockTime };

            foreach (var item in joined)
            {
                if (item.LockTime >= DateTime.UtcNow.AddMinutes(LocktimeMinutesAllowance))
                {
                    retList.Add(item.output);
                }
            }

            if (considerTimeOut)
            {
                return new Tuple<UniversalUnspentOutput[], bool, string>(retList.ToArray(),
                    ret.Item2, ret.Item3);
            }
            else
            {
                return ret;
            }
            */

            return ret;
        }

        public static async Task<Tuple<float, bool, string>> GetAccountBalance(string walletAddress,
            string assetId, Network network, Func<int> getMinimumConfirmationNumber = null)
        {
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    return await GetAccountBalanceQBitNinja(walletAddress, assetId, network, getMinimumConfirmationNumber);
                default:
                    throw new Exception("Not supported.");
            }
        }

        public static double GetAssetBalance(UniversalUnspentOutput[] outputs,
            string assetId, long multiplyFactor, Func<int> getMinimumConfirmationNumber = null)
        {
            double total = 0;

            getMinimumConfirmationNumber = getMinimumConfirmationNumber ?? DefaultGetMinimumConfirmationNumber;
            var minimumConfirmationNumber = getMinimumConfirmationNumber();

            foreach (var item in outputs)
            {
                if ((item.GetAssetId() != null && item.GetAssetId().Equals(assetId))
                    || (item.GetAssetId() == null && assetId.Trim().ToUpper().Equals("BTC")))
                {
                    if (item.GetConfirmationNumber() >= getMinimumConfirmationNumber())
                    {
                        if (item.GetAssetId() != null)
                        {
                            total += item.GetAssetAmount();
                        }
                        else
                        {
                            total += item.GetBitcoinAmount();
                        }
                    }
                }
            }

            return total / multiplyFactor;
        }

        public static bool IsBitcoinsEnough(UniversalUnspentOutput[] outputs,
            long amountInSatoshi, Func<int> getMinimumConfirmationNumber = null)
        {
            long total = 0;

            getMinimumConfirmationNumber = getMinimumConfirmationNumber ?? DefaultGetMinimumConfirmationNumber;
            var minimumConfirmationNumber = getMinimumConfirmationNumber();

            foreach (var item in outputs)
            {
                if (item.GetConfirmationNumber() >= getMinimumConfirmationNumber())
                {
                    total += item.GetBitcoinAmount();
                }
            }

            if (total >= amountInSatoshi)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks whether the amount for assetId of the wallet is enough
        /// </summary>
        /// <param name="walletAddress">Address of the wallet</param>
        /// <param name="assetId">Asset id to check the balance for.</param>
        /// <param name="amount">The required amount to check for.</param>
        /// <returns>Whether the asset amount is enough or not.</returns>
        public static async Task<bool> IsAssetsEnough(string walletAddress, string assetId,
            int amount, Network network, long multiplyFactor, Func<int> getMinimumConfirmationNumber = null)
        {
            Tuple<float, bool, string> result = await GetAccountBalance(walletAddress, assetId, network, getMinimumConfirmationNumber);
            if (result.Item2 == true)
            {
                return false;
            }
            else
            {
                if (result.Item1 >= amount)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool IsAssetsEnough(UniversalUnspentOutput[] outputs,
            string assetId, double assetAmount, long multiplyFactor, Func<int> getMinimumConfirmationNumber = null)
        {
            if (!string.IsNullOrEmpty(assetId))
            {
                double total = GetAssetBalance(outputs, assetId, multiplyFactor, getMinimumConfirmationNumber);

                if (total - assetAmount >= 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        public static long GetAssetBTCAmount(this string asset, double amount)
        {
            return !IsRealAsset(asset) ? Convert.ToInt64(amount * BTCToSathoshiMultiplicationFactor) : 0;
        }

        public static async Task<GetCoinsForWalletReturnType> GetCoinsForWallet
            (string multiSigAddress, long requiredSatoshiAmount, double requiredAssetAmount, string asset, AssetDefinition[] assets,
            RPCConnectionParams connectionParams, string connectionString, SqlexpressLykkeEntities entities,
            bool isOrdinaryReturnTypeRequired, bool isAddressMultiSig = true, Func<int> getMinimumConfirmationNumber = null, bool ignoreUnconfirmed = false)
        {
            GetCoinsForWalletReturnType ret;
            if (!IsRealAsset(asset))
            {
                requiredSatoshiAmount = (long)Math.Max(requiredSatoshiAmount, requiredAssetAmount * BTCToSathoshiMultiplicationFactor);
            }

            if (isOrdinaryReturnTypeRequired)
            {
                ret = new GetOrdinaryCoinsForWalletReturnType();
            }
            else
            {
                ret = new GetScriptCoinsForWalletReturnType();
            }

            try
            {
                if (isAddressMultiSig)
                {
                    ret.MatchingAddress = await GetMatchingMultisigAddress(multiSigAddress, entities);
                }

                // Getting wallet outputs
                var walletOutputs = await GetWalletOutputs
                    (multiSigAddress, connectionParams.BitcoinNetwork, entities, getMinimumConfirmationNumber, ignoreUnconfirmed);
                if (walletOutputs.Item2)
                {
                    ret.Error = new Error();
                    ret.Error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    ret.Error.Message = walletOutputs.Item3;
                }
                else
                {
                    var bitcoinOutputs = GetWalletOutputsUncolored(walletOutputs.Item1);
                    if (!IsBitcoinsEnough(bitcoinOutputs, requiredSatoshiAmount, getMinimumConfirmationNumber))
                    {
                        ret.Error = new Error();
                        ret.Error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                        ret.Error.Message = "The required amount of satoshis to send transaction is " + requiredSatoshiAmount +
                            " . The address is: " + multiSigAddress;
                    }
                    else
                    {
                        UniversalUnspentOutput[] assetOutputs = null;

                        if (IsRealAsset(asset))
                        {
                            ret.Asset = GetAssetFromName(assets, asset, connectionParams.BitcoinNetwork);
                            if (ret.Asset == null)
                            {
                                ret.Error = new Error();
                                ret.Error.Code = ErrorCode.AssetNotFound;
                                ret.Error.Message = "Could not find asset with name: " + asset;
                            }
                            else
                            {
                                // Getting the asset output to provide the assets
                                assetOutputs = GetWalletOutputsForAsset(walletOutputs.Item1, ret.Asset.AssetId);
                            }
                        }
                        if (IsRealAsset(asset) && ret.Asset != null && !IsAssetsEnough(assetOutputs, ret.Asset.AssetId, requiredAssetAmount, ret.Asset.AssetMultiplicationFactor, getMinimumConfirmationNumber))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughAssetAvailable;
                            ret.Error.Message = "The required amount of " + asset + " to send transaction is " + requiredAssetAmount +
                                " . The address is: " + multiSigAddress;
                        }
                        else
                        {
                            // Converting bitcoins to script coins so that we could sign the transaction
                            var coins = GetColoredUnColoredCoins(bitcoinOutputs, null).Item2;
                            if (coins.Length != 0)
                            {
                                if (isOrdinaryReturnTypeRequired)
                                {
                                    ((GetOrdinaryCoinsForWalletReturnType)ret).Coins = coins;
                                }
                                else
                                {
                                    ((GetScriptCoinsForWalletReturnType)ret).ScriptCoins = new ScriptCoin[coins.Length];
                                    for (int i = 0; i < coins.Length; i++)
                                    {
                                        ((GetScriptCoinsForWalletReturnType)ret).ScriptCoins[i] = new ScriptCoin(coins[i], new Script(ret.MatchingAddress.MultiSigScript));
                                    }
                                }
                            }

                            if (IsRealAsset(asset))
                            {
                                // Converting assets to script coins so that we could sign the transaction
                                var assetCoins = ret.Asset != null ? GetColoredUnColoredCoins(assetOutputs, ret.Asset.AssetId).Item1 : new ColoredCoin[0];

                                if (assetCoins.Length != 0)
                                {
                                    if (isOrdinaryReturnTypeRequired)
                                    {
                                        ((GetOrdinaryCoinsForWalletReturnType)ret).AssetCoins = assetCoins;
                                    }
                                    else
                                    {
                                        ((GetScriptCoinsForWalletReturnType)ret).AssetScriptCoins = new ColoredCoin[assetCoins.Length];
                                        for (int i = 0; i < assetCoins.Length; i++)
                                        {
                                            ((GetScriptCoinsForWalletReturnType)ret).AssetScriptCoins[i] = new ColoredCoin(assetCoins[i].Amount,
                                                new ScriptCoin(assetCoins[i].Bearer, new Script(ret.MatchingAddress.MultiSigScript)));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ret.Error = new Error();
                ret.Error.Code = ErrorCode.Exception;
                ret.Error.Message = e.ToString();
            }

            return ret;
        }

        internal class PrevOutput
        {
            public string Hash
            {
                get;
                set;
            }

            public uint N
            {
                get;
                set;
            }
        }


        public static async Task<bool> IsInputStillSpendable(TxIn input, string username,
            string password, string ipAddress, Network network)
        {
            var txId = input.PrevOut.Hash.ToString();
            var outputNumber = input.PrevOut.N;

            LykkeExtenddedRPCClient client = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential(username, password),
                            ipAddress, network);
            if (string.IsNullOrEmpty(await client.GetTxOut
                (input.PrevOut.Hash.ToString(), input.PrevOut.N)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static async Task<bool> IsRefundSpendable(string transactionHex, DateTimeOffset locktime,
            RPCConnectionParams connectionParams)
        {
            // We set the time for 24 hours since
            // 1- A transaction may be broadcasted but not get confirmed
            // 2- Then a refund gets broadcasted and gets confirmation
            // 3- Any transaction based on 1 is now could not get conirmation 
            // So we assume each transaction spending inputs of refund
            // before 24 hours of refund become spendable is in competition with
            // refund and should be waited for conirmation
            // In other words, This is to cover the following:
            // What happens if
            // 1- A transaction (T1) spends one of the inputs for Refund (R1)
            // 2- We report R1 as unspendable because of this
            // 3- T1 does not get confirmation and R1 becomes spendable.
            // So in this case if we spend another input of R1, we may face double spend
            // Although in our case we just will have swaps (as T1) what happens if 
            // R1 becomes spendable with a swap

            // Solution: We assume a broadcasted transaction gets confirmation 24 hours
            DateTime now = DateTime.UtcNow;
            if (locktime <= now.AddHours(24) && (await GetNumberOfTransactionConfirmations(transactionHex) < 3))
            {
                // Probably if we reach here GetNumberOfTransactionConfirmations(transactionHex) will always return 0 and second condition
                // will always be ture; Because if the refund is broadcasted its spent coins is not returned as free coins, so actual transactionHex
                // may not be required and transaction could be signed by exchange and returned to the client.
                return true;
            }
            else
            {
                return false;
            }
            /*
            DateTime now = DateTime.UtcNow;
            if (now.AddHours(-2) <= locktime && locktime < now.AddHours(24))
            {
                if (transactionHex != null)
                {
                    Transaction tx = new Transaction(transactionHex);
                    foreach (var input in tx.Inputs)
                    {
                        if (!(await IsInputStillSpendable(input, username, password, ipAddress, network)))
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
            else
            {
                return false;
            }
            */
        }

        static async Task AddRange<T>(
            this List<T> source, Task<IEnumerable<T>> destination)
        {
            source.AddRange(await destination);
        }

        // From http://stackoverflow.com/questions/14889988/how-can-i-use-where-with-an-async-predicate
        static async Task<IEnumerable<T>> Where<T>(
            this IEnumerable<T> source, Func<T, Task<bool>> predicate)
        {
            var results = new ConcurrentQueue<T>();
            var tasks = source.Select(
                async x =>
                {
                    if (await predicate(x))
                        results.Enqueue(x);
                });
            await Task.WhenAll(tasks);
            return results;
        }


        public class SpentRefund
        {
            public long[] NewRefundId
            {
                get;
                set;
            }

            public long[] OldRefundId
            {
                get;
                set;
            }
        }

        public static async Task<SpentRefund> DoesSpendRefund(this Transaction tx,
            SqlexpressLykkeEntities entities, RPCConnectionParams connectionParams)
        {
            var prevOutputs = tx.Inputs.Select(i => new PrevOutput
            {
                Hash = i.PrevOut.Hash.ToString(),
                N = i.PrevOut.N
            });

            return await Task.Run(async () =>
            {
                SpentRefund spentRefund = new SpentRefund();

                var spendsAWholeRefund =
                (await ((from prevOutput in prevOutputs
                         join spentOutput in entities.WholeRefundSpentOutputs on new { Hash = prevOutput.Hash, N = prevOutput.N }
                         equals new { Hash = spentOutput.SpentTransactionId, N = (uint)spentOutput.SpentTransactionOutputNumber }
                         select spentOutput)
                         .Where(async (o) => await IsRefundSpendable(o.WholeRefund.TransactionHex, o.WholeRefund.LockTime, connectionParams)))).Select(r => r.WholeRefund.id)?.Distinct();


                spentRefund.NewRefundId = spendsAWholeRefund?.ToArray();

                var spendsAPartialRefund =
                (await (from prevOutput in prevOutputs
                        join spentOutput in entities.RefundedOutputs on new { Hash = prevOutput.Hash, N = prevOutput.N }
                        equals new { Hash = spentOutput.RefundedTxId, N = (uint)spentOutput.RefundedOutputNumber }
                        select spentOutput)
                        .Where(async (o) => await IsRefundSpendable(o.RefundTransaction.RefundTxHex, o.LockTime, connectionParams))).Select(r => r.RefundTransaction.id)?.Distinct();

                spentRefund.OldRefundId = spendsAPartialRefund?.ToArray();

                return spentRefund;
            });
        }

        public class SentTransactionReturnValue
        {
            public Error Error
            {
                get;
                set;
            }

            public long SentTransactionId
            {
                get;
                set;
            }
        }

        public static async Task<SentTransactionReturnValue> CheckTransactionForDoubleSpentBothSignaturesRequired(Transaction tx,
            RPCConnectionParams connectionParams, SqlexpressLykkeEntities entitiesContext, string connectionString,
            HandleTxRequest handleTxRequest, IPreBroadcastHandler preBroadcastHandler, Action outsideTransactionBeforeBroadcast = null,
            Action<SqlexpressLykkeEntities> databaseCommitableAction = null,
            APIProvider apiProvider = APIProvider.QBitNinja, bool isCompletelySignedTransaction = true)
        {
            return await CheckTransactionForDoubleSpentThenSendItCore(tx, connectionParams, entitiesContext, connectionString,
                handleTxRequest, outsideTransactionBeforeBroadcast, databaseCommitableAction, apiProvider,
                true, true, preBroadcastHandler);
        }

        public static async Task<SentTransactionReturnValue> CheckTransactionForDoubleSpentClientSignatureRequired(Transaction tx,
            RPCConnectionParams connectionParams, SqlexpressLykkeEntities entitiesContext, string connectionString,
            HandleTxRequest handleTxRequest, IPreBroadcastHandler preBroadcastHandler,
            Action outsideTransactionBeforeBroadcast = null, Action<SqlexpressLykkeEntities> databaseCommitableAction = null,
            APIProvider apiProvider = APIProvider.QBitNinja, bool isCompletelySignedTransaction = true)
        {
            return await CheckTransactionForDoubleSpentThenSendItCore(tx, connectionParams, entitiesContext, connectionString,
                handleTxRequest, outsideTransactionBeforeBroadcast, databaseCommitableAction, apiProvider,
                true, false, preBroadcastHandler);
        }

        public static async Task<SentTransactionReturnValue> CheckTransactionForDoubleSpentThenSendIt(Transaction tx,
            RPCConnectionParams connectionParams, SqlexpressLykkeEntities entitiesContext, string connectionString,
            HandleTxRequest handleTxRequest, IPreBroadcastHandler preBroadcastHandler, Action outsideTransactionBeforeBroadcast = null,
            Action<SqlexpressLykkeEntities> databaseCommitableAction = null,
            APIProvider apiProvider = APIProvider.QBitNinja)
        {
            return await CheckTransactionForDoubleSpentThenSendItCore(tx, connectionParams, entitiesContext, connectionString,
                handleTxRequest, outsideTransactionBeforeBroadcast, databaseCommitableAction, apiProvider,
                false, false, preBroadcastHandler);
        }

        // ToDo - Performance should be revisted by possible join operation
        internal static async Task<SentTransactionReturnValue> CheckTransactionForDoubleSpentThenSendItCore(Transaction tx,
            RPCConnectionParams connectionParams, SqlexpressLykkeEntities entitiesContext, string connectionString,
            HandleTxRequest handleTxRequest, Action outsideTransactionBeforeBroadcast,
            Action<SqlexpressLykkeEntities> databaseCommitableAction,
            APIProvider apiProvider, bool isClientSignatureRequiredOnTransaction,
            bool isExchangeSignatureRequiredOnTransaction,
            IPreBroadcastHandler preBroadcastHandler)
        {
            Error error = null;

            bool isCompletelySignedTransaction = !(isClientSignatureRequiredOnTransaction || isExchangeSignatureRequiredOnTransaction);

            long sentTransactionId = 0;

            // Checking if the inputs has been already spent
            // ToDo - Performance should be revisted by possible join operation
            foreach (var item in tx.Inputs)
            {
                string prevOut = item.PrevOut.Hash.ToString();
                var spentTx = await (from uxto in entitiesContext.SpentOutputs
                                     join dbTx in entitiesContext.SentTransactions on uxto.SentTransactionId equals dbTx.id
                                     where uxto != null && uxto.PrevHash.Equals(prevOut.ToString()) && uxto.OutputNumber.Equals((int)item.PrevOut.N)
                                     select dbTx.TransactionHex).FirstOrDefaultAsync();

                if (spentTx != null)
                {
                    error = new Error();
                    error.Code = ErrorCode.PossibleDoubleSpend;
                    error.Message = "The output number " + item.PrevOut.N + " from transaction " + item.PrevOut.Hash +
                        " has been already spent in transaction " + (new Transaction(spentTx)).GetHash();
                    break;
                }
            }

            if (error == null)
            {
                // Performing the required action here
                if (outsideTransactionBeforeBroadcast != null)
                {
                    outsideTransactionBeforeBroadcast.Invoke();
                }

                // Then marking the inputs as spent
                if (databaseCommitableAction != null)
                {
                    databaseCommitableAction.Invoke(entitiesContext);
                }

                SentTransaction dbSentTransaction = new SentTransaction
                {
                    TransactionHex = tx.ToHex(),
                    TransactionId = tx.GetHash().ToString(),
                    CreationDate = DateTime.UtcNow,
                    IsClientSignatureRequired = isClientSignatureRequiredOnTransaction,
                    IsExchangeSignatureRequired = isExchangeSignatureRequiredOnTransaction
                };
                entitiesContext.SentTransactions.Add(dbSentTransaction);
                await entitiesContext.SaveChangesAsync();
                sentTransactionId = dbSentTransaction.id;

                foreach (var item in tx.Inputs)
                {
                    entitiesContext.SpentOutputs.Add(new SpentOutput
                    {
                        OutputNumber = (int)item.PrevOut.N,
                        PrevHash = item.PrevOut.Hash.ToString(),
                        SentTransactionId = dbSentTransaction.id
                    });
                }
                await entitiesContext.SaveChangesAsync();

                var spentRefund =
                    await DoesSpendRefund(tx, entitiesContext, connectionParams);
                if ((spentRefund.NewRefundId != null && spentRefund.NewRefundId.Length > 0) ||
                    (spentRefund.OldRefundId != null && spentRefund.OldRefundId.Length > 0))
                {
                    foreach (var item in spentRefund?.NewRefundId?.Distinct())
                    {
                        entitiesContext.TransactionsWaitForConfirmations.Add
                            (new TransactionsWaitForConfirmation
                            { txToBeWatched = tx.GetHash().ToString(), OldRefundedTxId = null, WholeRefundId = item });
                    }

                    foreach (var item in spentRefund?.OldRefundId?.Distinct())
                    {
                        entitiesContext.TransactionsWaitForConfirmations.Add
                            (new TransactionsWaitForConfirmation
                            { txToBeWatched = tx.GetHash().ToString(), OldRefundedTxId = item, WholeRefundId = null });
                    }
                }
                await entitiesContext.SaveChangesAsync();


                // ToDo: Complete the whole refund function
                /*
                if (await tx.DoesSpendRefund(entitiesContext))
                {

                }
                */

                if (isCompletelySignedTransaction)
                {
                    // Notifing the web api of the transaction
                    // The server should respond to: curl -X Post http://localhost:8088/HandledTx -H "Content-Type: application/json" -d "{\"TransactionId\" : \"transactionId\", \"BlockchainHash\" : \"blockchainHash\",\"Operation\" : \"Transfer\" }"
                    if (handleTxRequest != null && preBroadcastHandler != null)
                    {
                        var errResponse = await preBroadcastHandler.HandleTx(handleTxRequest);

                        if (errResponse != null)
                        {
                            throw new Exception(string.Format("Error while notifing Lykke Jobs. Error code: {0} and Error Message: {1}",
                                errResponse.ErrorCode, errResponse.ErrorMessage));
                        }
                    }


                    // Database is successful, only the commit has remained. Broadcating the transaction
                    RPCClient client = new RPCClient(new System.Net.NetworkCredential(connectionParams.Username, connectionParams.Password),
                        connectionParams.IpAddress, connectionParams.BitcoinNetwork);

                    await client.SendRawTransactionAsync(tx);

                    // Waiting until the transaction has appeared in block explorer
                    // This is because some consequent operaions like generating refund may instantly
                    // be called and the new transaction has not been propagaed
                    // If the appearence does not take place after some retries, the
                    // current function returns successfuly, since the responsibility of
                    // the current function is to send the transaction and not the rest
                    /*
                    bool breakFor = false;
                    for (int i = 0; i < 10; i++)
                    {
                        switch (apiProvider)
                        {
                            case APIProvider.QBitNinja:
                                bool isPresent = await IsTransactionPresentInQBitNinja(tx);
                                if (isPresent)
                                {
                                    breakFor = true;
                                    break;
                                }
                                else
                                {
                                    System.Threading.Thread.Sleep(500);
                                }
                                break;
                            default:
                                breakFor = true;
                                break;
                        }
                        if (breakFor)
                        {
                            break;
                        }
                    }
                    */
                    try
                    {
                        await IsTransactionFullyIndexed(tx, connectionParams, entitiesContext);
                    }
                    catch (Exception)
                    {
                        // No exception should be thrown after tx has been sent to blockchain
                    }
                }
            }

            return new SentTransactionReturnValue { Error = error, SentTransactionId = sentTransactionId };
        }

        public static async Task IsTransactionFullyIndexed(Transaction tx, RPCConnectionParams connectionParams,
            SqlexpressLykkeEntities entities, bool confirmationRequired = false)
        {
            var settings = new TheSettings { QBitNinjaBaseUrl = OpenAssetsHelper.QBitNinjaBaseUrl };

            try
            {
                await WaitUntillQBitNinjaHasIndexed(settings, HasTransactionIndexed,
                    new string[] { tx.GetHash().ToString() }, null, entities);
            }
            catch (Exception)
            {
            }

            var destAddresses = tx.Outputs.Select(o => o.ScriptPubKey.GetDestinationAddress(connectionParams.BitcoinNetwork)?.ToWif()).Where(c => !string.IsNullOrEmpty(c)).Distinct();
            foreach (var addr in destAddresses)
            {
                try
                {
                    if (confirmationRequired)
                    {
                        await WaitUntillQBitNinjaHasIndexed(settings, HasBalanceIndexed,
                            new string[] { tx.GetHash().ToString() }, addr, entities);
                    }
                    else
                    {
                        await WaitUntillQBitNinjaHasIndexed(settings, HasBalanceIndexedZeroConfirmation,
                            new string[] { tx.GetHash().ToString() }, addr, entities);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public static async Task<Tuple<GenerateMassOutputsTaskResult, Error>> GenerateMassOutputs(TaskToDoGenerateMassOutputs data, string purpose,
            RPCConnectionParams connectionParams, string connectionString,
            AssetDefinition[] assets, string feeAddress, string feeAddressPrivateKey)
        {
            GenerateMassOutputsTaskResult result = null;
            Error error = null;
            string destinationAddress = null;
            string destinationAddressPrivateKey = null;
            string assetId = null;
            if (purpose.Trim().ToLower().Equals("fee"))
            {
                destinationAddress = feeAddress;
                destinationAddressPrivateKey = feeAddressPrivateKey;
            }
            else
            {
                var split = purpose.Trim().Split(new char[] { ':' });
                if (split[0].ToLower().Equals("asset"))
                {
                    var asset = assets.Where(a => a.Name.Equals(split[1])).Select(a => a).FirstOrDefault();
                    if (asset != null)
                    {
                        destinationAddress = asset.AssetAddress;
                        destinationAddressPrivateKey = asset.PrivateKey;
                        assetId = asset.AssetId;
                    }
                }
            }
            if (destinationAddress == null)
            {
                error = new Error();
                error.Code = ErrorCode.BadInputParameter;
                error.Message = "The specified purpose is invalid.";
            }
            else
            {
                try
                {
                    using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
                    {
                        var outputs = await GetWalletOutputs(data.WalletAddress, connectionParams.BitcoinNetwork, entities);
                        if (outputs.Item2)
                        {
                            error = new Error();
                            error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                            error.Message = outputs.Item3;
                        }
                        else
                        {
                            var uncoloredOutputs = GetWalletOutputsUncolored(outputs.Item1);
                            double totalRequiredAmount = data.Count * data.FeeAmount * BTCToSathoshiMultiplicationFactor; // Convert to satoshi
                            double minimumRequiredAmountForParticipation = Convert.ToInt64(0.001 * BTCToSathoshiMultiplicationFactor);
                            var output = uncoloredOutputs.Where(o => (o.GetValue() > minimumRequiredAmountForParticipation)).ToList();
                            if (output.Count == 0)
                            {
                                error = new Error();
                                error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                                error.Message = "There is no output available with the minimum amount of: " + minimumRequiredAmountForParticipation.ToString("n") + " satoshis.";
                            }
                            else
                            {
                                if (output.Select(o => (long)o.GetValue()).Sum() < totalRequiredAmount)
                                {
                                    error = new Error();
                                    error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                                    error.Message = "The sum of total applicable outputs is less than the required: " + totalRequiredAmount.ToString("n") + " satoshis.";
                                }
                                else
                                {
                                    var sourceCoins = output.Select(o => new Coin(new uint256(o.GetTransactionHash()), (uint)o.GetOutputIndex(),
                                        o.GetValue(), new Script(StringToByteArray(o.GetScriptHex()))));
                                    TransactionBuilder builder = new TransactionBuilder();
                                    //builder.DustPrevention = false;
                                    if (PrivateKeyWillBeSubmitted)
                                    {
                                        builder.AddKeys(new BitcoinSecret(GetPrivateKeyForAddress(data.WalletAddress)));
                                    }
                                    else
                                    {
                                        builder.AddKeys(new BitcoinSecret(data.PrivateKey));
                                    }
                                    builder.AddCoins(sourceCoins);
                                    builder.SetChange(new BitcoinPubKeyAddress(data.WalletAddress, connectionParams.BitcoinNetwork));
                                    for (int i = 0; i < data.Count; i++)
                                    {
                                        builder.Send(new BitcoinPubKeyAddress(destinationAddress, connectionParams.BitcoinNetwork),
                                            new Money(Convert.ToInt64(data.FeeAmount * BTCToSathoshiMultiplicationFactor)))
                                            .BuildTransaction(false);
                                    }

                                    var fee = (Convert.ToInt64(builder.EstimateSize(builder.BuildTransaction(false))
                                        * TransactionSendFeesInSatoshi)) / 1000;
                                    Transaction tx = builder.SendFees(Math.Max(fee, TransactionSendFeesInSatoshi)).
                                        BuildTransaction(true);
                                    IList<PreGeneratedOutput> preGeneratedOutputs = null;

                                    using (var transaction = entities.Database.BeginTransaction())
                                    {
                                        Error localerror = (await CheckTransactionForDoubleSpentThenSendIt
                                                    (tx, connectionParams, entities, connectionString, null,
                                                    null, null, (entitiesContext) =>
                                                   {
                                                       var tId = tx.GetHash().ToString();
                                                       preGeneratedOutputs = new List<PreGeneratedOutput>();
                                                       for (int i = 0; i < tx.Outputs.Count; i++)
                                                       {
                                                           var item = tx.Outputs[i];
                                                           if (item.Value.Satoshi != Convert.ToInt64(data.FeeAmount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor))
                                                           {
                                                               continue;
                                                           }
                                                           PreGeneratedOutput f = new PreGeneratedOutput();
                                                           f.TransactionId = tId;
                                                           f.OutputNumber = i;
                                                           f.Script = item.ScriptPubKey.ToHex();
                                                           f.PrivateKey = destinationAddressPrivateKey;
                                                           f.Amount = item.Value.Satoshi;
                                                           f.AssetId = assetId;
                                                           f.Address = destinationAddress;
                                                           f.Network = connectionParams.Network;
                                                           preGeneratedOutputs.Add(f);
                                                       }

                                                       entitiesContext.PreGeneratedOutputs.AddRange(preGeneratedOutputs);
                                                   })).Error;
                                        if (localerror == null)
                                        {
                                            result = new GenerateMassOutputsTaskResult
                                            {
                                                TransactionHash = tx.GetHash().ToString()
                                            };
                                        }
                                        else
                                        {
                                            error = localerror;
                                        }

                                        if (error == null)
                                        {
                                            transaction.Commit();
                                        }
                                        else
                                        {
                                            transaction.Rollback();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    error = new Error();
                    error.Code = ErrorCode.Exception;
                    error.Message = e.ToString();
                }
            }
            return new Tuple<GenerateMassOutputsTaskResult, Error>(result, error);
        }

        #region BasicStructures
        public class Asset
        {
            public string AssetId
            {
                get;
                set;
            }

            public BitcoinAddress AssetAddress
            {
                get;
                set;
            }

            public long AssetMultiplicationFactor
            {
                get;
                set;
            }

            public string AssetDefinitionUrl { get; set; }

            public string AssetPrivateKey { get; set; }
        }

        public class GetCoinsForWalletReturnType
        {
            public Error Error
            {
                get;
                set;
            }

            public KeyStorage MatchingAddress
            {
                get;
                set;
            }

            public Asset Asset { get; set; }
        }

        public class GetScriptCoinsForWalletReturnType : GetCoinsForWalletReturnType
        {
            public ColoredCoin[] AssetScriptCoins
            {
                get;
                set;
            }

            public ScriptCoin[] ScriptCoins
            {
                get;
                set;
            }
        }

        public class GetOrdinaryCoinsForWalletReturnType : GetCoinsForWalletReturnType
        {
            public ColoredCoin[] AssetCoins
            {
                get;
                set;
            }

            public Coin[] Coins
            {
                get;
                set;
            }
        }
        #endregion

        #region OtherUsefulFunctions
        // From http://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-an-object-in-net-c-specifically
        public static T DeepClone<T>(T obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T)formatter.Deserialize(ms);
            }
        }
        public static void AddPrivateKey(string privateKey, bool isP2PKH)
        {
            var secret = new BitcoinSecret(privateKey);
            var p2pkhAddr = secret.GetAddress();
            if (P2PKHDictionary.ContainsKey(p2pkhAddr.ToString()))
            {
                return;
            }
            P2PKHDictionary.AddThreadSafe(p2pkhAddr.ToWif(), privateKey);

            if (!isP2PKH)
            {
                var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { secret.PubKey ,
                secret.PubKey.GetExchangePrivateKey(null, WebSettings.ConnectionString).PubKey });
                var multiSigAddressStorage = multiSigAddress.GetScriptAddress(Network).ToString();

                MultisigDictionary.AddThreadSafe(multiSigAddressStorage, privateKey);
                MultisigScriptDictionary.AddThreadSafe(multiSigAddressStorage, multiSigAddress.ToString());
            }
        }
        // This is written as a function instead of a constant since we may need to change the implementation
        // to adaptable one in future
        public static async Task<FeeRate> GetFeeRate()
        {
            if (TransactionSendFeesInSatoshi == 0)
            {
                if ((TransactionSendFeesInSatoshi = await UpdateFeeRateFromInternet()) == 0)
                {
                    TransactionSendFeesInSatoshi = DefaultTransactionSendFeesInSatoshi;
                }
            }

            TransactionSendFeesInSatoshi = FeeMultiplicationFactor * Math.Min(TransactionSendFeesInSatoshi,
                MaximumTransactionSendFeesInSatoshi);
            return new FeeRate(new Money(TransactionSendFeesInSatoshi));
        }

        public class TwentyOneFeeReport
        {
            public int fastestFee
            {
                get;
                set;
            }

            public int halfHourFee
            {
                get;
                set;
            }

            public int hourFee
            {
                get;
                set;
            }
        }

        public static async Task<uint> UpdateFeeRateFromInternet()
        {
            string url = "https://bitcoinfees.21.co/api/v1/fees/recommended";
            try
            {
                using (HttpClient webClient = new HttpClient())
                {
                    var str = await webClient.GetStringAsync(url);
                    var deserialize = JsonConvert.DeserializeObject<TwentyOneFeeReport>(str);
                    int feeToUse = 0;
                    switch (FeeType)
                    {
                        case FeeType21co.FastestFee:
                            feeToUse = deserialize.fastestFee;
                            break;
                        case FeeType21co.HalfHourFee:
                            feeToUse = deserialize.halfHourFee;
                            break;
                        case FeeType21co.HourFee:
                            feeToUse = deserialize.hourFee;
                            break;
                        default:
                            feeToUse = deserialize.halfHourFee;
                            break;
                    }
                    return (uint)Math.Max(feeToUse * 1000, MinimumTransactionSendFeesInSatoshi);
                }
            }
            catch (Exception e)
            {
                return Math.Max(TransactionSendFeesInSatoshi, MinimumTransactionSendFeesInSatoshi);
            }
        }

        public static TransactionBuilder BuildHalfOfSwap(this TransactionBuilder builder, BitcoinSecret[] secret, ScriptCoin[] uncoloredCoins, ColoredCoin[] coloredCoins, BitcoinScriptAddress destAddress,
            BitcoinScriptAddress changeAddress, AssetMoney coloredAmount, long uncoloredAmount, string asset, out long coloredCoinCount)
        {
            coloredCoinCount = 0;

            if (secret != null)
            {
                builder
                .AddKeys(secret);
            }

            if (IsRealAsset(asset))
            {
                builder
                    .AddCoins(coloredCoins)
                    .SendAssetWithChange(destAddress, coloredAmount, coloredCoins, changeAddress);
                coloredCoinCount = 2;
            }
            else
            {
                builder
                    .AddCoins(uncoloredCoins)
                    .SendWithChange(destAddress, uncoloredAmount, uncoloredCoins, changeAddress);
            }

            return builder;
        }

        /*
        private static async Task<Transaction[]> GetTransactionsHex(UniversalUnspentOutput[] outputList, Network network,
            string username, string password, string ipAddress)
        {
            Transaction[] walletTransactions = new Transaction[outputList.Length];
            for (int i = 0; i < walletTransactions.Length; i++)
            {
                var ret = await GetTransactionHex(outputList[i].GetTransactionHash(), network, username, password, ipAddress);
                if (!ret.Item1)
                {
                    walletTransactions[i] = new Transaction(ret.Item3);
                }
                else
                {
                    throw new Exception("Could not get the transaction hex for the transaction with id: "
                        + outputList[i].GetTransactionHash() + " . The exact error message is " + ret.Item2);
                }
            }
            return walletTransactions;
        }
        */

        public static string GetAddressFromScriptPubKey(Script scriptPubKey, Network network)
        {
            string address = null;
            if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
            {
                address = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey).GetAddress(network).ToWif().ToString();
            }
            else
            {
                if (PayToScriptHashTemplate.Instance.CheckScriptPubKey(scriptPubKey))
                {
                    address = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey).GetAddress(network).ToWif().ToString();
                }
            }

            return address;
        }

        // The returned object is a Tuple with first parameter specifing if an error has occured,
        // second the error message and third the transaction hex
        public static async Task<Tuple<bool, string, string>> GetTransactionHex(string transactionId,
            RPCConnectionParams connectionParams)
        {
            string transactionHex = "";
            bool errorOccured = false;
            string errorMessage = "";
            try
            {
                RPCClient client = new RPCClient(new System.Net.NetworkCredential(connectionParams.Username, connectionParams.Password),
                                connectionParams.IpAddress, connectionParams.BitcoinNetwork);
                transactionHex = (await client.GetRawTransactionAsync(uint256.Parse(transactionId), true)).ToHex();
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<bool, string, string>(errorOccured, errorMessage, transactionHex);
        }

        public static async Task PerformFunctionEndJobs(string connectionString, ILog log,
            string inputMessage, string outputMessage)
        {
            try
            {
                await SendPendingEmailsAndLogInputOutput(connectionString, inputMessage, outputMessage);
            }
            catch (Exception e)
            {
                if (log != null)
                {
                    await log.WriteError("OpenAssetsHelper", "ServiceLykkeWallet", "", e);
                }
            }
        }

        public static async Task SendPendingEmailsAndLogInputOutput(string connectionString, string inputMessage, string outputMessage)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
            {
                var emails = from item in entities.EmailMessages
                             select item;

                if (emails.Count() > 0)
                {
                    foreach (var item in emails)
                    {
                        await EmailQueueWriter.PutMessageAsync(item.Message);
                    }

                    entities.EmailMessages.RemoveRange(emails);

                    await entities.SaveChangesAsync();
                }

                entities.InputOutputMessageLogs.Add(new InputOutputMessageLog
                {
                    InputMessage = inputMessage,
                    OutputMessage = outputMessage,
                    CreationDate = DateTime.UtcNow
                });
                await entities.SaveChangesAsync();
            }
        }

        // PlainTextBroadcast:{"Data":{"BroadcastGroup":100,"MessageData":{"Subject":"Some subject","Text":"Some text"}}}
        public static async Task SendAlertForPregenerateOutput(string assetId, int count,
            int minimumCount, SqlexpressLykkeEntities entities)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Hello,");
            builder.AppendLine(string.Format("The number of pre generated outputs ({0}) for asset {1} has fallen below minimum {2}.",
                count, assetId ?? "BTC", minimumCount));
            builder.AppendLine("Please add some pre generatad outputs.");
            if (!string.IsNullOrEmpty(EnvironmentName))
            {
                builder.Append("The current environment is: ");
                builder.AppendLine(EnvironmentName);
            }
            builder.AppendLine("Best,");
            builder.AppendLine("PreGenerated Watchdog");
            string message = builder.ToString();

            var ptb = new PlainTextBroadcast();
            ptb.Data = new PlainTextBroadcastData();
            ptb.Data.BroadcastGroup = BroadcastGroup;
            ptb.Data.MessageData = new PlainTextMessage();
            ptb.Data.MessageData.Subject = "PreGenerated Output Starvation!";
            ptb.Data.MessageData.Text = message;

            entities.EmailMessages.Add(new EmailMessage { Message = "PlainTextBroadcast:" + JsonConvert.SerializeObject(ptb) });
            await entities.SaveChangesAsync();
            //await EmailQueueWriter.PutMessageAsync();
        }

        public static async Task<bool> PregeneratedHasBeenSpentInBlockchain(PreGeneratedOutput p, RPCConnectionParams connectionParams)
        {
            LykkeExtenddedRPCClient client = new LykkeExtenddedRPCClient(new System.Net.NetworkCredential(connectionParams.Username, connectionParams.Password),
                            connectionParams.IpAddress, connectionParams.BitcoinNetwork);
            if (string.IsNullOrEmpty(await client.GetTxOut(p.TransactionId, (uint)p.OutputNumber)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static async Task CreateNewReserve(SqlexpressLykkeEntities entities, string reserveId, PreGeneratedOutput c, bool save)
        {
            PregeneratedReserve reserve = new PregeneratedReserve();

            reserve.PreGeneratedOutputTxId = c.TransactionId;
            reserve.PreGeneratedOutputN = c.OutputNumber;
            reserve.CreationTime = DateTime.UtcNow;
            reserve.ReserveId = reserveId;

            entities.PregeneratedReserves.Add(reserve);
            if (save)
            {
                await entities.SaveChangesAsync();
            }
        }

        public static async Task<PreGeneratedOutput> GetOnePreGeneratedOutput(SqlexpressLykkeEntities entities,
            RPCConnectionParams connectionParams, long minimumSatoshiOfTheOutput = 0, string assetId = null, string reservedForAddress = null, string reserveId = null)
        {
            bool pregeneratedCoinFound = false;

            if (!string.IsNullOrEmpty(reservedForAddress))
            {
                var reservedCoins = (from item in entities.PreGeneratedOutputs
                                     where item.Consumed.Equals(0) && item.Network.Equals(connectionParams.Network) && item.Amount >= minimumSatoshiOfTheOutput &&
                                     (assetId == null ? item.AssetId == null : item.AssetId.Equals(assetId.ToString())) &&
                                     item.ReservedForAddress == reservedForAddress
                                     select item).ToList();

                foreach (var c in reservedCoins)
                {
                    var isPreviouslyUsed = (from item in entities.PregeneratedReserves
                                            where item.PreGeneratedOutputN == c.OutputNumber
                                            && item.PreGeneratedOutputTxId == c.TransactionId
                                            && item.ReserveId == reserveId
                                            select item).Any();

                    if (isPreviouslyUsed)
                    {
                        continue;
                    }
                    else
                    {
                        await CreateNewReserve(entities, reserveId, c, true);

                        return c;
                    }
                }
            }

            var coins = from item in entities.PreGeneratedOutputs
                        where item.Consumed.Equals(0) && item.Network.Equals(connectionParams.Network) && item.Amount >= minimumSatoshiOfTheOutput && (assetId == null ? item.AssetId == null : item.AssetId.Equals(assetId.ToString())) && string.IsNullOrEmpty(item.ReservedForAddress)
                        select item;

            int count = await coins.CountAsync();

            PreGeneratedOutput f = null;
            while (!pregeneratedCoinFound)
            {
                if (count < PreGeneratedOutputMinimumCount)
                {
                    await SendAlertForPregenerateOutput(assetId, count, PreGeneratedOutputMinimumCount, entities);
                }

                if (count == 0)
                {
                    throw new Exception(string.Format("There is no proper coins to use for fee payment. Either no coin or no coin with at least {0} satoshi.",
                        minimumSatoshiOfTheOutput));
                }
                else
                {
                    int index = new Random().Next(coins.Count());
                    f = await coins.OrderBy(c => c.TransactionId).Skip(index).Take(1).FirstAsync();
                    if (string.IsNullOrEmpty(reserveId))
                    {
                        f.Consumed = 1;
                    }
                    else
                    {
                        f.ReservedForAddress = reservedForAddress;
                        await CreateNewReserve(entities, reserveId, f, false);
                    }
                    await entities.SaveChangesAsync();

                    if (!await PregeneratedHasBeenSpentInBlockchain(f, connectionParams))
                    {
                        pregeneratedCoinFound = true;
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(reserveId))
                        {
                            f.Consumed = 1;
                            f.ReservedForAddress = null;
                            await entities.SaveChangesAsync();
                        }
                    }
                }
            }

            return f;
        }

        public static async Task CheckForFeeOutputAndFree(SqlexpressLykkeEntities entities, UnsignedTransactionSpentOutput item)
        {
            var feeOutput = (from fee in entities.PreGeneratedOutputs
                             where fee.TransactionId == item.TransactionId && fee.OutputNumber == item.OutputNumber
                             select fee).FirstOrDefault();

            if (feeOutput != null)
            {
                feeOutput.ReservedForAddress = null;
                await entities.SaveChangesAsync();
            }
        }

        public static string GetPrivateKeyForAddress(string address)
        {
            Exception exp = new Exception("Could not find a matching record for address: "
                    + address);
            var parsedAddr = Base58Data.GetFromBase58Data(address);
            if (parsedAddr is BitcoinColoredAddress)
            {
                parsedAddr = ((BitcoinColoredAddress)parsedAddr).Address;
            }
            if (parsedAddr is BitcoinAddress)
            {
                if (P2PKHDictionary.ContainsKey(parsedAddr.ToWif()))
                {
                    return P2PKHDictionary[parsedAddr.ToWif()];
                }
                else
                {
                    throw exp;
                }
            }
            if (parsedAddr is BitcoinScriptAddress)
            {
                if (MultisigDictionary.ContainsKey(parsedAddr.ToWif()))
                {
                    return MultisigDictionary[parsedAddr.ToWif()];
                }
                else
                {
                    throw exp;
                }
            }

            throw new Exception(string.Format("Could not parse the address {0} successfully.", address));
        }

        public static async Task<PubKey> GetClientPubKeyForMultisig(string multisigAddr, SqlexpressLykkeEntities entities)
        {
            var multisig = await GetMatchingMultisigAddress(multisigAddr, entities);
            return (new BitcoinSecret(multisig.WalletPrivateKey)).PubKey;
        }


        public static async Task<KeyStorage> GetMatchingMultisigAddress(string multiSigAddress, SqlexpressLykkeEntities entities)
        {
            KeyStorage ret = null;

            var base58Data = Base58Data.GetFromBase58Data(multiSigAddress);
            if (base58Data is BitcoinColoredAddress)
            {
                multiSigAddress = (base58Data as BitcoinColoredAddress).Address.ToWif();
            }

            if (PrivateKeyWillBeSubmitted)
            {
                if (MultisigDictionary.ContainsKey(multiSigAddress))
                {
                    ret = new KeyStorage();
                    ret.ExchangePrivateKey = (new BitcoinSecret(MultisigDictionary[multiSigAddress])).PubKey.GetExchangePrivateKey(entities).ToWif();
                    ret.MultiSigAddress = multiSigAddress;
                    ret.MultiSigScript = MultisigScriptDictionary[multiSigAddress];
                    ret.Network = Network.ToString();
                    ret.WalletPrivateKey = MultisigDictionary[multiSigAddress];
                    ret.WalletAddress = (new BitcoinSecret(ret.WalletPrivateKey)).GetAddress().ToWif();
                }
            }
            else
            {
                ret = await (from item in entities.KeyStorages
                             where item.MultiSigAddress.Equals(multiSigAddress)
                             select item).SingleOrDefaultAsync();
            }

            if (ret == null)
            {
                throw new Exception("Could not find a matching record for MultiSigAddress: "
                    + multiSigAddress);
            }

            return ret;
        }

        public static bool IsRealAsset(string asset)
        {
            if (asset != null && asset.Trim().ToUpper() != "BTC")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static Asset GetAssetFromName(AssetDefinition[] assets, string assetName, Network network)
        {
            Asset ret = null;
            foreach (var item in assets)
            {
                if (item.Name == assetName)
                {
                    ret = new Asset();
                    ret.AssetId = item.AssetId;
                    ret.AssetPrivateKey = item.PrivateKey;
                    ret.AssetAddress = BitcoinAddress.Create(item.AssetAddress, network);
                    ret.AssetMultiplicationFactor = item.MultiplyFactor;
                    ret.AssetDefinitionUrl = item.DefinitionUrl;
                    break;
                }
            }

            return ret;
        }

        // From: http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static BitcoinAddress GetBitcoinAddressFormBase58Date(string base58Data)
        {
            var base58Decoded = Base58Data.GetFromBase58Data(base58Data);
            var address = base58Decoded as BitcoinAddress;
            if (address != null)
            {
                return address;
            }
            else
            {
                return (base58Decoded as BitcoinColoredAddress)?.Address;
            }
        }

        #endregion

        #region EmailStructure
        public class PlainTextMessage
        {
            public string Subject
            {
                get;
                set;
            }

            public string Text
            {
                get;
                set;
            }
        }

        public class PlainTextBroadcastData
        {
            public int BroadcastGroup
            {
                get;
                set;
            }

            public PlainTextMessage MessageData
            {
                get;
                set;
            }
        }

        public class PlainTextBroadcast
        {
            public PlainTextBroadcastData Data
            {
                get;
                set;
            }
        }
        #endregion

        //#region LykkeJobsNotificationStructures
        //public class LykkeJobsNotificationMessage
        //{
        //    public string TransactionId
        //    {
        //        get;
        //        set;
        //    }

        //    public string BlockchainHash
        //    {
        //        get;
        //        set;
        //    }

        //    public string Operation
        //    {
        //        get;
        //        set;
        //    }
        //}

        //public class LykkeJobsNotificationResponseError
        //{
        //    public int Code
        //    {
        //        get;
        //        set;
        //    }

        //    public string Message
        //    {
        //        get;
        //        set;
        //    }
        //}
        //public class LykkeJobsNotificationResponse
        //{
        //    public LykkeJobsNotificationResponseError Error
        //    {
        //        get;
        //        set;
        //    }
        //}
        //#endregion

        #region BitcoinApiReturnStructure
        public class UniversalUnspentOutput
        {
        }

        public class QBitNinjaUnspentOutput : UniversalUnspentOutput
        {
            public string transaction_hash { get; set; }
            public int output_index { get; set; }
            public long value { get; set; }
            public int confirmations { get; set; }
            public string script_hex { get; set; }
            public string asset_id { get; set; }
            public long asset_quantity { get; set; }
        }

        public class QBitNinjaReceivedCoin
        {
            public string transactionId { get; set; }
            public int index { get; set; }
            public long value { get; set; }
            public string scriptPubKey { get; set; }
            public object redeemScript { get; set; }
            public string assetId { get; set; }
            public long quantity { get; set; }
        }

        public class QBitNinjaSpentCoin
        {
            public string address { get; set; }
            public string transactionId { get; set; }
            public int index { get; set; }
            public long value { get; set; }
            public string scriptPubKey { get; set; }
            public object redeemScript { get; set; }
            public string assetId { get; set; }
            public long quantity { get; set; }
        }

        public class QBitNinjaOperation
        {
            public long amount { get; set; }
            public int confirmations { get; set; }
            public int height { get; set; }
            public string blockId { get; set; }
            public string transactionId { get; set; }
            public List<QBitNinjaReceivedCoin> receivedCoins { get; set; }
            public List<QBitNinjaSpentCoin> spentCoins { get; set; }
        }



        public class QBitNinjaOutputResponse
        {
            public object continuation { get; set; }
            public List<QBitNinjaOperation> operations { get; set; }
        }

        public class QBitNinjaBlock
        {
            public string blockId { get; set; }
            public string blockHeader { get; set; }
            public int height { get; set; }
            public int confirmations { get; set; }
            public string medianTimePast { get; set; }
            public string blockTime { get; set; }
        }

        public class QBitNinjaTransactionResponse
        {
            public string transaction { get; set; }
            public string transactionId { get; set; }
            public bool isCoinbase { get; set; }
            public QBitNinjaBlock block { get; set; }
            public List<QBitNinjaSpentCoin> spentCoins { get; set; }
            public List<QBitNinjaReceivedCoin> receivedCoins { get; set; }
            public string firstSeen { get; set; }
            public int fees { get; set; }
        }

        public class BlockCypherInput
        {
            public string prev_hash { get; set; }
            public int output_index { get; set; }
            public string script { get; set; }
            public long output_value { get; set; }
            public object sequence { get; set; }
            public string[] addresses { get; set; }
            public string script_type { get; set; }
        }

        public class BlockCypherOutput
        {
            public long value { get; set; }
            public string script { get; set; }
            public string spent_by { get; set; }
            public string[] addresses { get; set; }
            public string script_type { get; set; }
        }

        public class BlockCypherGetTransactionResult
        {
            public string block_hash { get; set; }
            public int block_height { get; set; }
            public int block_index { get; set; }
            public string hash { get; set; }
            public string hex { get; set; }
            public string[] addresses { get; set; }
            public long total { get; set; }
            public int fees { get; set; }
            public int size { get; set; }
            public string preference { get; set; }
            public string relayed_by { get; set; }
            public string confirmed { get; set; }
            public string received { get; set; }
            public int ver { get; set; }
            public int lock_time { get; set; }
            public bool double_spend { get; set; }
            public int vin_sz { get; set; }
            public int vout_sz { get; set; }
            public int confirmations { get; set; }
            public int confidence { get; set; }
            public BlockCypherInput[] inputs { get; set; }
            public BlockCypherOutput[] outputs { get; set; }
        }
        #endregion

        #region QBitNinjaFunctions
        public static async Task<bool> IsTransactionPresentInQBitNinja(Transaction tx)
        {
            string url = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    url = QBitNinjaTransactionUrl + tx.GetHash().ToString();
                    HttpResponseMessage result = await client.GetAsync(url);
                    if (!result.IsSuccessStatusCode)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static async Task<Tuple<float, bool, string>> GetAccountBalanceQBitNinja(string walletAddress,
            string assetId, Network network, Func<int> getMinimumConfirmationNumber = null)
        {
            float balance = 0;
            float unconfirmedBalance = 0;
            bool errorOccured = false;
            string errorMessage = "";
            string url;

            getMinimumConfirmationNumber = getMinimumConfirmationNumber ?? DefaultGetMinimumConfirmationNumber;
            var minimumConfirmationNumber = getMinimumConfirmationNumber();

            try
            {
                QBitNinjaOutputResponse response = null;

                /*
                using (HttpClient client = new HttpClient())
                {
                    url = QBitNinjaBalanceUrl + walletAddress;
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        return new Tuple<float, bool, string>(0, true, result.ReasonPhrase);
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        response = JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                    }
                }
                */
                Tuple<float, bool, string> retValue = null;
                response = await GetAddressBalance(walletAddress,
                    (result) => { retValue = new Tuple<float, bool, string>(0, true, result.ReasonPhrase); }
                    , true, true);
                if (retValue != null)
                {
                    return retValue;
                }


                if (response.operations != null && response.operations.Count > 0)
                {
                    foreach (var item in response.operations)
                    {
                        response.operations.ForEach((o) =>
                        {
                            balance += o.receivedCoins.Where(c => !string.IsNullOrEmpty(c.assetId) && c.assetId.Equals(assetId) && o.confirmations >= getMinimumConfirmationNumber()).Select(c => c.quantity).Sum();
                            unconfirmedBalance += o.receivedCoins.Where(c => !string.IsNullOrEmpty(c.assetId) && c.assetId.Equals(assetId) && o.confirmations == 0).Select(c => c.quantity).Sum();
                        });
                    }
                }
                else
                {
                    errorOccured = true;
                    errorMessage = "No coins found.";
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<float, bool, string>(balance, errorOccured, errorMessage);
        }

        public static async Task<QBitNinjaOutputResponse> GetAddressBalance(string walletAddress, Action<HttpResponseMessage> onNotSuccessfulReturn,
            bool colored = true, bool unspentonly = true, bool ignoreUnconfirmed = false)
        {
            string continuation = null;
            List<QBitNinjaOperation> operations = new List<QBitNinjaOperation>();

            do
            {
                QBitNinjaOutputResponse notProcessedUnspentOutputs = null;
                using (HttpClient client = new HttpClient())
                {
                    string url = null;
                    url = string.Format("{0}?unspentonly={1}&colored={2}",
                        QBitNinjaBalanceUrl + walletAddress, unspentonly.ToString().ToLower(), colored.ToString().ToLower());
                    if (ignoreUnconfirmed)
                    {
                        string blockNumberUrl = string.Format("{0}/blocks/tip?headeronly=true",
                            QBitNinjaBaseUrl);
                        HttpResponseMessage blockNumberResult = await client.GetAsync(blockNumberUrl);
                        if (!blockNumberResult.IsSuccessStatusCode)
                        {
                            onNotSuccessfulReturn.Invoke(blockNumberResult);
                            return null;
                        }
                        else
                        {
                            var webResponse = await blockNumberResult.Content.ReadAsStringAsync();
                            dynamic blockNumberUnprocessed = JObject.Parse(webResponse);
                            int blockHeight = blockNumberUnprocessed.additionalInformation.height;
                            url += string.Format("&from={0}", blockHeight);
                        }
                    }
                    if (!string.IsNullOrEmpty(continuation))
                    {
                        url += string.Format("&continuation={0}", continuation);
                    }
                    HttpResponseMessage result = await client.GetAsync(url);

                    if (!result.IsSuccessStatusCode)
                    {
                        onNotSuccessfulReturn.Invoke(result);
                        return null;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                        operations.AddRange(notProcessedUnspentOutputs.operations);
                        continuation = notProcessedUnspentOutputs.continuation as string;
                    }
                }
            }
            while (!string.IsNullOrEmpty(continuation));

            return new QBitNinjaOutputResponse { continuation = null, operations = operations };
        }

        public static async Task<int> GetNumberOfTransactionConfirmations(string txId)
        {
            string url = null;
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    url = QBitNinjaTransactionUrl + txId;
                    HttpResponseMessage result = await client.GetAsync(url);

                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaTransactionResponse>
                                (webResponse);
                        return response.block.confirmations;
                    }
                    else
                    {
                        return -1;
                    }
                }
            }
            catch (Exception exp)
            {
                return -1;
            }
        }

        public static async Task<bool> HasTransactionPassedItsWaitTime(string txId,
            SqlexpressLykkeEntities entitiesContext)
        {
            var found = (from item in entitiesContext.TransactionsWaitForConfirmations
                         where item.txToBeWatched == txId
                         select item).FirstOrDefault();

            if (found != null)
            {
                if (await GetNumberOfTransactionConfirmations(txId) > 2)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        delegate Task PopulateUnspentOupt(QBitNinjaOperation operation);

        public static async Task<Tuple<QBitNinjaUnspentOutput[], bool, string>> GetWalletOutputsQBitNinja(string walletAddress,
            Network network, SqlexpressLykkeEntities entitiesContext, Func<int> getMinimumConfirmationNumber = null, bool ignoreUnconfirmed = false)
        {
            bool errorOccured = false;
            string errorMessage = string.Empty;
            // List is not thread safe to be runned in parallel
            //IList<QBitNinjaUnspentOutput> unspentOutputsList = new List<QBitNinjaUnspentOutput>();
            ConcurrentQueue<QBitNinjaUnspentOutput> unspentOutputConcurrent = new ConcurrentQueue<QBitNinjaUnspentOutput>();

            getMinimumConfirmationNumber = getMinimumConfirmationNumber ?? DefaultGetMinimumConfirmationNumber;
            var minimumConfirmationNumber = getMinimumConfirmationNumber();

            try
            {
                QBitNinjaOutputResponse notProcessedUnspentOutputs = null;
                /*
                using (HttpClient client = new HttpClient())
                {
                    string url = null;
                    url = QBitNinjaBalanceUrl + walletAddress;
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        errorOccured = true;
                        errorMessage = result.ReasonPhrase;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                    }
                }
                */
                notProcessedUnspentOutputs = await GetAddressBalance(walletAddress,
                    (result) => { errorOccured = true; errorMessage = result.ReasonPhrase; }
                    , true, true, ignoreUnconfirmed);

                if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                {
                    PopulateUnspentOupt t = async (o) =>
                    {
                        var convertResult = o.receivedCoins.Select(c => new QBitNinjaUnspentOutput
                        {
                            confirmations = o.confirmations,
                            output_index = c.index,
                            transaction_hash = c.transactionId,
                            value = c.value,
                            script_hex = c.scriptPubKey,
                            asset_id = c.assetId,
                            asset_quantity = c.quantity
                        });

                        /*
                        await ((List<QBitNinjaUnspentOutput>)unspentOutputsList).AddRange(
                            convertResult.Where(async (u) => (u.confirmations >= getMinimumConfirmationNumber() && await HasTransactionPassedItsWaitTime(u.transaction_hash, entitiesContext))));
                            */

                        (await convertResult.Where(async (u) => (u.confirmations >= getMinimumConfirmationNumber() && await HasTransactionPassedItsWaitTime(u.transaction_hash, entitiesContext))))
                        .ToList().ForEach(c => unspentOutputConcurrent.Enqueue(c));
                    };
                    var tasks = notProcessedUnspentOutputs.operations.Select(o => t(o));
                    await Task.WhenAll(tasks);
                    /*
                    notProcessedUnspentOutputs.operations.ForEach(async (o) =>
                    {
                        var convertResult = o.receivedCoins.Select(c => new QBitNinjaUnspentOutput
                        {
                            confirmations = o.confirmations,
                            output_index = c.index,
                            transaction_hash = c.transactionId,
                            value = c.value,
                            script_hex = c.scriptPubKey,
                            asset_id = c.assetId,
                            asset_quantity = c.quantity
                        });

                        try
                        {
                            await ((List<QBitNinjaUnspentOutput>)unspentOutputsList).AddRange(
                                convertResult.Where(async (u) => (u.confirmations >= getMinimumConfirmationNumber() && await HasTransactionPassedItsWaitTime(u.transaction_hash, entitiesContext))));
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    });
                    */
                }
                else
                {
                    errorOccured = true;
                    errorMessage = "No coins to retrieve.";
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }

            // return new Tuple<QBitNinjaUnspentOutput[], bool, string>(unspentOutputsList.ToArray(), errorOccured, errorMessage);
            return new Tuple<QBitNinjaUnspentOutput[], bool, string>(unspentOutputConcurrent.ToArray(),
                errorOccured, errorMessage);
        }

        #endregion

        #region PropagationVerification

        public static async Task<bool> HasBalanceIndexed(TheSettings settings, string txId, string btcAddress)
        {
            return await HasBalanceIndexedInternal(settings, txId, btcAddress);
        }

        public static async Task<bool> HasBalanceIndexedZeroConfirmation(TheSettings settings, string txId, string btcAddress)
        {
            return await HasBalanceIndexedInternal(settings, txId, btcAddress, false);
        }

        public static async Task<bool> HasBalanceIndexedInternal(TheSettings settings, string txId, string btcAddress,
            bool confirmationRequired = true)
        {
            HttpResponseMessage result = null;
            bool exists = false;
            using (HttpClient client = new HttpClient())
            {
                string url = null;
                exists = false;
                url = settings.QBitNinjaBaseUrl + "balances/" + btcAddress + "?unspentonly=true&colored=true";
                result = await client.GetAsync(url);
            }

            if (!result.IsSuccessStatusCode)
            {
                return false;
            }
            else
            {
                var webResponse = await result.Content.ReadAsStringAsync();
                var notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                    (webResponse);
                if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                {
                    notProcessedUnspentOutputs.operations.ForEach((o) =>
                    {
                        exists = o.receivedCoins
                       .Where(c => c.transactionId.Equals(txId) && (!confirmationRequired | o.confirmations > 0))
                       .Any() | exists;
                        if (exists)
                        {
                            return;
                        }
                    });
                }

                return exists;
            }
        }

        public static async Task<bool> IsUrlSuccessful(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage result = await client.GetAsync(url);
                if (!result.IsSuccessStatusCode)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static async Task<bool> HasTransactionIndexed(TheSettings settings, string txId, string dummy)
        {
            string url = settings.QBitNinjaBaseUrl + "transactions/" + txId + "?colored=true";
            return await IsUrlSuccessful(url);
        }

        public static async Task<bool> HasBlockIndexed(TheSettings settings, string blockId, string dummy)
        {
            string url = settings.QBitNinjaBaseUrl + "blocks/" + blockId + "?headeronly=true";
            return await IsUrlSuccessful(url);
        }

        public static async Task WaitUntillQBitNinjaHasIndexed(TheSettings settings,
            Func<TheSettings, string, string, Task<bool>> checkIndexed, IEnumerable<string> ids, string id2 = null,
            SqlexpressLykkeEntities entities = null)
        {
            var indexed = false;
            foreach (var id in ids)
            {
                indexed = false;
                for (int i = 0; i < 30; i++)
                {
                    bool result = false;
                    try
                    {
                        result = await checkIndexed(settings, id, id2);
                    }
                    catch (Exception exp)
                    {
                        if (entities != null)
                        {
                            await GeneralLogger.WriteError("OpenAssetsHelper", string.Empty, string.Empty, exp, null, entities);
                        }
                    }

                    if (result)
                    {
                        indexed = true;
                        break;
                    }
                    await Task.Delay(1000);
                }

                if (!indexed)
                {
                    throw new Exception(string.IsNullOrEmpty(id2) ? string.Format("Item with id: {0} did not get indexed yet.", id) : string.Format("Item with id: {0} did not get indexed yet. Provided id2 is {1}", id, id2));
                }
            }
        }

        #endregion
    }
}
