using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public static class OpenAssetsHelper
    {
        private enum APIProvider
        {
            CoinPrism,
            QBitNinja
        }

        const string coinprismBaseUrl = "https://api.coinprism.com/v1/addresses/";
        const string coinprismTestnetBaseUrl = "https://testnet.api.coinprism.com/v1/addresses/";
        /*
        const string qbitNinjaBaseUrl = "http://localhost:85/balances/";
        const string qbitNinjaTestnetBaseUrl = "http://localhost:85/balances/";
        */

        public const uint MinimumRequiredSatoshi = 50000; // 100000000 satoshi is one BTC
        public const uint TransactionSendFeesInSatoshi = 10000;
        public const ulong BTCToSathoshiMultiplicationFactor = 100000000;
        public const uint ConcurrencyRetryCount = 3;
        public const uint NBitcoinColoredCoinOutputInSatoshi = 2730;
        private const APIProvider apiProvider = APIProvider.QBitNinja;
        private const int LocktimeMinutesAllowance = 120;

        public static string QBitNinjaBaseUrl
        {
            get;
            set;
        }


        public class AssetDefinition
        {
            public string AssetId { get; set; }
            public string AssetAddress { get; set; }
            public string Name { get; set; }
            public string PrivateKey { get; set; }
            public string DefinitionUrl { get; set; }
            public int Divisibility { get; set; }
            public long MultiplyFactor
            {
                get
                {
                    return (long)Math.Pow(10, Divisibility);
                }
            }
        }

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

        public static async Task<TransactionBuilder> AddEnoughPaymentFee(this TransactionBuilder builder, SqlexpressLykkeEntities entities,
            string network, int requiredNumberOfColoredCoinFee = 1)
        {
            var requiredFee = TransactionSendFeesInSatoshi + requiredNumberOfColoredCoinFee * NBitcoinColoredCoinOutputInSatoshi;
            long totalAddedFee = 0;

            while (true)
            {
                if (totalAddedFee >= requiredFee)
                {
                    break;
                }

                PreGeneratedOutput feePayer = await GetOnePreGeneratedOutput(entities, network);
                Coin feePayerCoin = feePayer.GetCoin();

                totalAddedFee += feePayer.Amount;
                builder.AddKeys(new BitcoinSecret(feePayer.PrivateKey)).AddCoins(feePayerCoin);
            }

            return builder;
        }

        /*
        public static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(UniversalUnspentOutput[] walletOutputs,
            string assetId, Network network, string username, string password, string ipAddress)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    return await GetColoredUnColoredCoins(walletOutputs != null ? walletOutputs.Select(c => (CoinprismUnspentOutput)c).ToArray() : null, assetId,
                        network, username, password, ipAddress);
                case APIProvider.QBitNinja:
                    return await GetColoredUnColoredCoins(walletOutputs != null ? walletOutputs.Select(c => (QBitNinjaUnspentOutput)c).ToArray() : null, assetId,
                        network, username, password, ipAddress);
                default:
                    throw new Exception("We should never reach here.");
            }
        }
        */

        public static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(UniversalUnspentOutput[] walletOutputs,
            string assetId, Network network, string username, string password, string ipAddress)
        {
            var walletAssetOutputs = GetWalletOutputsForAsset(walletOutputs, assetId);
            var walletUncoloredOutputs = GetWalletOutputsUncolored(walletOutputs);
            var walletColoredTransactions = await GetTransactionsHex(walletAssetOutputs, network, username, password, ipAddress);
            var walletUncoloredTransactions = await GetTransactionsHex(walletUncoloredOutputs, network, username, password, ipAddress);
            var walletColoredCoins = GenerateWalletColoredCoins(walletColoredTransactions, walletAssetOutputs, assetId);
            var walletUncoloredCoins = GenerateWalletUnColoredCoins(walletUncoloredTransactions, walletUncoloredOutputs);
            return new Tuple<ColoredCoin[], Coin[]>(walletColoredCoins, walletUncoloredCoins);
        }

        /*
        private static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(QBitNinjaUnspentOutput[] walletOutputs,
            string assetId, Network network, string username, string password, string ipAddress)
        {
            var walletAssetOutputs = GetWalletOutputsForAsset(walletOutputs, assetId);
            var walletUncoloredOutputs = GetWalletOutputsUncolored(walletOutputs);
            var walletColoredTransactions = await GetTransactionsHex(walletAssetOutputs, network, username, password, ipAddress);
            var walletUncoloredTransactions = await GetTransactionsHex(walletUncoloredOutputs, network, username, password, ipAddress);
            var walletColoredCoins = GenerateWalletColoredCoins(walletColoredTransactions, walletAssetOutputs, assetId);
            var walletUncoloredCoins = GenerateWalletUnColoredCoins(walletUncoloredTransactions, walletUncoloredOutputs);
            return new Tuple<ColoredCoin[], Coin[]>(walletColoredCoins, walletUncoloredCoins);
        }
        */
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

        /*
        public static UniversalUnspentOutput[] GetWalletOutputsUncolored(UniversalUnspentOutput[] input)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    var coinPrismResult = GetWalletOutputsUncolored(input != null ? input.Select(c => (CoinprismUnspentOutput)c).
                        ToArray() : null);
                    return coinPrismResult != null ? coinPrismResult.Select(c => (UniversalUnspentOutput)c).ToArray() : null;
                case APIProvider.QBitNinja:
                    var qbitNinjaResult = GetWalletOutputsUncolored(input != null ? input.Select(c => (QBitNinjaUnspentOutput)c).
                        ToArray() : null);
                    return qbitNinjaResult != null ? qbitNinjaResult.Select(c => (UniversalUnspentOutput)c).ToArray() : null;
                default:
                    throw new Exception("Not supported");
            }
        }

        // ToDo: At the time of this todo I have not addressed, colored coins
        private static QBitNinjaUnspentOutput[] GetWalletOutputsUncolored(QBitNinjaUnspentOutput[] input)
        {
            IList<QBitNinjaUnspentOutput> outputs = new List<QBitNinjaUnspentOutput>();
            foreach (var item in input)
            {
                if (item.asset_id == null)
                {
                    outputs.Add(item);
                }
            }

            return outputs.ToArray();
        }
        private static CoinprismUnspentOutput[] GetWalletOutputsUncolored(CoinprismUnspentOutput[] input)
        {
            IList<CoinprismUnspentOutput> outputs = new List<CoinprismUnspentOutput>();
            foreach (var item in input)
            {
                if (item.asset_id == null)
                {
                    outputs.Add(item);
                }
            }

            return outputs.ToArray();
        }
        */

        public static UniversalUnspentOutput[] GetWalletOutputsForAsset(UniversalUnspentOutput[] input, string assetId)
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

        /*
        public static CoinprismUnspentOutput[] GetWalletOutputsForAsset(CoinprismUnspentOutput[] input, string assetId)
        {
            IList<CoinprismUnspentOutput> outputs = new List<CoinprismUnspentOutput>();
            if (assetId != null)
            {
                foreach (var item in input)
                {
                    if (item.asset_id == assetId)
                    {
                        outputs.Add(item);
                    }
                }
            }

            return outputs.ToArray();
        }
        */

        public static async Task<Tuple<UniversalUnspentOutput[], bool, string>> GetWalletOutputs(string walletAddress,
            Network network, SqlexpressLykkeEntities entities, bool considerTimeOut = true)
        {
            Tuple<UniversalUnspentOutput[], bool, string> ret = null;
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    var coinprismResult = await GetWalletOutputsCoinPrism(walletAddress, network);
                    ret = new Tuple<UniversalUnspentOutput[], bool, string>(coinprismResult.Item1 != null ? coinprismResult.Item1.Select(c => (UniversalUnspentOutput)c).ToArray() : null,
                        coinprismResult.Item2, coinprismResult.Item3);
                    break;
                case APIProvider.QBitNinja:
                    var qbitResult = await GetWalletOutputsQBitNinja(walletAddress, network);
                    ret = new Tuple<UniversalUnspentOutput[], bool, string>(qbitResult.Item1 != null ? qbitResult.Item1.Select(c => (UniversalUnspentOutput)c).ToArray() : null,
                        qbitResult.Item2, qbitResult.Item3);
                    break;
                default:
                    throw new Exception("Not supported.");
            }

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
        }

        private static async Task<Tuple<QBitNinjaUnspentOutput[], bool, string>> GetWalletOutputsQBitNinja(string walletAddress,
            Network network)
        {
            bool errorOccured = false;
            string errorMessage = string.Empty;
            IList<QBitNinjaUnspentOutput> unspentOutputsList = new List<QBitNinjaUnspentOutput>();

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = null;
                    if (network == Network.Main)
                    {
                        url = QBitNinjaBaseUrl + walletAddress;
                    }
                    else
                    {
                        url = QBitNinjaBaseUrl + walletAddress;
                    }
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        errorOccured = true;
                        errorMessage = result.ReasonPhrase;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        var notProcessedUnspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaUnspentOutputResponse>
                            (webResponse);
                        if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                        {
                            notProcessedUnspentOutputs.operations.ForEach((o) =>
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
                                ((List<QBitNinjaUnspentOutput>)unspentOutputsList).AddRange(convertResult);
                            });
                        }
                        else
                        {
                            errorOccured = true;
                            errorMessage = "No coins to retrieve.";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }

            return new Tuple<QBitNinjaUnspentOutput[], bool, string>(unspentOutputsList.ToArray(), errorOccured, errorMessage);
        }


        private static async Task<Tuple<CoinprismUnspentOutput[], bool, string>> GetWalletOutputsCoinPrism(string walletAddress,
            Network network)
        {
            bool errorOccured = false;
            string errorMessage = string.Empty;
            CoinprismUnspentOutput[] unspentOutputs = null;

            // ToDo - We currently use coinprism api, later we should replace
            // with our self implementation
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = null;
                    if (network == Network.Main)
                    {
                        url = coinprismBaseUrl + walletAddress;
                    }
                    else
                    {
                        url = coinprismTestnetBaseUrl + walletAddress;
                    }
                    HttpResponseMessage result = await client.GetAsync(url + "/unspents");
                    if (!result.IsSuccessStatusCode)
                    {
                        errorOccured = true;
                        errorMessage = result.ReasonPhrase;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        unspentOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<CoinprismUnspentOutput[]>
                            (webResponse);
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }

            return new Tuple<CoinprismUnspentOutput[], bool, string>(unspentOutputs, errorOccured, errorMessage);
        }

        public static async Task<Tuple<float, float, bool, string>> GetAccountBalance(string walletAddress,
            string assetId, Network network)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    return await GetAccountBalanceCoinPrism(walletAddress, assetId, network);
                case APIProvider.QBitNinja:
                    return await GetAccountBalanceQBitNinja(walletAddress, assetId, network);
                default:
                    throw new Exception("Not supported.");
            }
        }

        // ToDo: confirmation number is set to be 1
        public static async Task<Tuple<float, float, bool, string>> GetAccountBalanceQBitNinja(string walletAddress,
            string assetId, Network network)
        {
            float balance = 0;
            float unconfirmedBalance = 0;
            bool errorOccured = false;
            string errorMessage = "";
            string url;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (network == Network.Main)
                    {
                        url = QBitNinjaBaseUrl + walletAddress;
                    }
                    else
                    {
                        url = QBitNinjaBaseUrl + walletAddress;
                    }
                    HttpResponseMessage result = await client.GetAsync(url + "?unspentonly=true&colored=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        return new Tuple<float, float, bool, string>(0, 0, true, result.ReasonPhrase);
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        QBitNinjaUnspentOutputResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaUnspentOutputResponse>
                            (webResponse);
                        if (response.operations != null && response.operations.Count > 0)
                        {
                            foreach (var item in response.operations)
                            {
                                response.operations.ForEach((o) =>
                                {
                                    balance += o.receivedCoins.Where(c => !string.IsNullOrEmpty(c.assetId) && c.assetId.Equals(assetId) && o.confirmations > 0).Select(c => c.quantity).Sum();
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
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<float, float, bool, string>(balance, unconfirmedBalance, errorOccured, errorMessage);
        }

        /// <summary>
        /// Gets the asset balance for the wallet address
        /// </summary>
        /// <param name="walletAddress">The address of the wallet to check the balance for.</param>
        /// <param name="assetId">The id of asset to check the balance.</param>
        /// <returns>A tuple, first part is balance, second part is unconfirmed balance, third part is whether error has occured or not,
        ///  forth part is the error message.</returns>
        public static async Task<Tuple<float, float, bool, string>> GetAccountBalanceCoinPrism(string walletAddress,
            string assetId, Network network)
        {
            float balance = 0;
            float unconfirmedBalance = 0;
            bool errorOccured = false;
            string errorMessage = "";
            string url;
            // ToDo - We currently use coinprism api, later we should replace
            // with our self implementation
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (network == Network.Main)
                    {
                        url = coinprismBaseUrl + walletAddress;
                    }
                    else
                    {
                        url = coinprismTestnetBaseUrl + walletAddress;
                    }
                    HttpResponseMessage result = await client.GetAsync(url);
                    if (!result.IsSuccessStatusCode)
                    {
                        return new Tuple<float, float, bool, string>(0, 0, true, result.ReasonPhrase);
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        CoinprismGetBalanceResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<CoinprismGetBalanceResponse>
                            (webResponse);
                        foreach (var item in response.assets)
                        {
                            if (item.id.Equals(assetId))
                            {
                                balance = float.Parse(item.balance);
                                unconfirmedBalance = float.Parse(item.unconfirmed_balance);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<float, float, bool, string>(balance, unconfirmedBalance, errorOccured, errorMessage);
        }

        public static long GetValue(this UniversalUnspentOutput output)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)output).value;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)output).transaction_hash;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)output).script_hex;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)output).output_index;
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)output).output_index;
                default:
                    throw new Exception("Not supported.");
            }
        }
        /*
        public static float GetAssetBalance(UniversalUnspentOutput[] outputs,
            string assetId, long multiplyFactor, bool includeUnconfirmed = false)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    return GetAssetBalance(outputs.Select(c => (CoinprismUnspentOutput)c).ToArray(), assetId, multiplyFactor, includeUnconfirmed);
                case APIProvider.QBitNinja:
                    return GetAssetBalance(outputs.Select(c => (QBitNinjaUnspentOutput)c).ToArray(), assetId, multiplyFactor, includeUnconfirmed);
                default:
                    throw new Exception("Not supported.");
            }
        }
        */
        private static string GetAssetId(this UniversalUnspentOutput item)
        {
            switch (apiProvider)
            {
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)item).asset_id;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)item).confirmations;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)item).asset_quantity;
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
                case APIProvider.CoinPrism:
                    return ((CoinprismUnspentOutput)item).value;
                case APIProvider.QBitNinja:
                    return ((QBitNinjaUnspentOutput)item).value;
                default:
                    throw new Exception("Not supported.");
            }
        }

        // ToDo - Clear confirmation number
        public static float GetAssetBalance(UniversalUnspentOutput[] outputs,
            string assetId, long multiplyFactor, bool includeUnconfirmed = false)
        {
            float total = 0;
            foreach (var item in outputs)
            {
                if ((item.GetAssetId() != null && item.GetAssetId().Equals(assetId))
                    || (item.GetAssetId() == null && assetId.Trim().ToUpper().Equals("BTC")))
                {
                    if (item.GetConfirmationNumber() == 0)
                    {
                        if (includeUnconfirmed)
                        {
                            if (item.GetAssetId() != null)
                            {
                                total += (float)item.GetAssetAmount();
                            }
                            else
                            {
                                total += item.GetBitcoinAmount();
                            }
                        }
                    }
                    else
                    {
                        if (item.GetAssetId() != null)
                        {
                            total += (float)item.GetAssetAmount();
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

        public static bool IsAssetsEnough(UniversalUnspentOutput[] outputs,
            string assetId, float assetAmount, long multiplyFactor, bool includeUnconfirmed = false)
        {
            if (!string.IsNullOrEmpty(assetId))
            {
                float total = GetAssetBalance(outputs, assetId, multiplyFactor, includeUnconfirmed);
                if (total >= assetAmount)
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

        // ToDo - Clear confirmation number
        public static bool IsBitcoinsEnough(UniversalUnspentOutput[] outputs,
            long amountInSatoshi, bool includeUnconfirmed = false)
        {
            long total = 0;
            foreach (var item in outputs)
            {
                if (item.GetConfirmationNumber() == 0)
                {
                    if (includeUnconfirmed)
                    {
                        total += item.GetBitcoinAmount();
                    }
                }
                else
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
        /// ToDo - Figure out a method for unconfirmed balance
        public static async Task<bool> IsAssetsEnough(string walletAddress, string assetId,
            int amount, Network network, long multiplyFactor, bool includeUnconfirmed = false)
        {
            Tuple<float, float, bool, string> result = await GetAccountBalance(walletAddress, assetId, network);
            if (result.Item3 == true)
            {
                return false;
            }
            else
            {
                if (!includeUnconfirmed)
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
                else
                {
                    if (result.Item1 + result.Item2 >= amount)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        // The returned object is a Tuple with first parameter specifing if an error has occured,
        // second the error message and third the transaction hex
        public static async Task<Tuple<bool, string, string>> GetTransactionHex(string transactionId, Network network,
            string username, string password, string ipAddress)
        {
            string transactionHex = "";
            bool errorOccured = false;
            string errorMessage = "";
            try
            {
                RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password),
                                ipAddress, network);
                transactionHex = (await client.GetRawTransactionAsync(uint256.Parse(transactionId), true)).ToHex();
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }
            return new Tuple<bool, string, string>(errorOccured, errorMessage, transactionHex);
        }

        // ToDo - Performance should be revisted by possible join operation
        public static async Task<Error> CheckTransactionForDoubleSpentThenSendIt(Transaction tx,
            string username, string password, string ipAddress, Network network, SqlexpressLykkeEntities entitiesContext, string connectionString,
            Action outsideTransactionBeforeBroadcast = null, Action<SqlexpressLykkeEntities> databaseCommitableAction = null)
        {
            Error error = null;
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
                        " has been already spent in transcation " + (new Transaction(spentTx)).GetHash();
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
                    TransactionHex = tx.ToHex()
                };
                entitiesContext.SentTransactions.Add(dbSentTransaction);
                await entitiesContext.SaveChangesAsync();

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

                // Database is successful, only the commit has remained. Broadcating the transaction
                RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password),
                    ipAddress, network);

                await client.SendRawTransactionAsync(tx);

            }

            return error;
        }

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

        public static async Task<GetCoinsForWalletReturnType> GetCoinsForWallet
            (string multiSigAddress, long requiredSatoshiAmount, float requiredAssetAmount, string asset, AssetDefinition[] assets,
            Network network, string username, string password, string ipAddress, string connectionString, SqlexpressLykkeEntities entities,
            bool isOrdinaryReturnTypeRequired, bool isAddressMultiSig = true)
        {
            GetCoinsForWalletReturnType ret;
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
                    (multiSigAddress, network, entities);
                if (walletOutputs.Item2)
                {
                    ret.Error = new Error();
                    ret.Error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    ret.Error.Message = walletOutputs.Item3;
                }
                else
                {
                    // Getting bitcoin outputs to provide the transaction fee
                    var bitcoinOutputs = GetWalletOutputsUncolored(walletOutputs.Item1);
                    if (!IsBitcoinsEnough(bitcoinOutputs, requiredSatoshiAmount))
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
                            ret.Asset = GetAssetFromName(assets, asset, network);
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
                        if (IsRealAsset(asset) && ret.Asset != null && !IsAssetsEnough(assetOutputs, ret.Asset.AssetId, requiredAssetAmount, ret.Asset.AssetMultiplicationFactor))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughAssetAvailable;
                            ret.Error.Message = "The required amount of " + asset + " to send transaction is " + requiredAssetAmount +
                                " . The address is: " + multiSigAddress;
                        }
                        else
                        {
                            // Converting bitcoins to script coins so that we could sign the transaction
                            var coins = (await GetColoredUnColoredCoins(bitcoinOutputs, null, network,
                                username, password, ipAddress)).Item2;
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
                                var assetCoins = ret.Asset != null ? (await GetColoredUnColoredCoins(assetOutputs, ret.Asset.AssetId, network,
                                username, password, ipAddress)).Item1 : new ColoredCoin[0];

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

        public static async Task<PreGeneratedOutput> GetOnePreGeneratedOutput(SqlexpressLykkeEntities entities,
            string network, string assetId = null)
        {
            var coins = from item in entities.PreGeneratedOutputs
                        where item.Consumed.Equals(0) && item.Network.Equals(network) && (assetId == null ? item.AssetId == null : item.AssetId.Equals(assetId.ToString()))
                        select item;

            int count = await coins.CountAsync();
            if (count == 0)
            {
                throw new Exception("There is no coins to use for fee payment");
            }
            else
            {
                int index = new Random().Next(coins.Count());
                PreGeneratedOutput f = await coins.OrderBy(c => c.TransactionId).Skip(index).Take(1).FirstAsync();
                f.Consumed = 1;
                await entities.SaveChangesAsync();
                return f;
            }
        }

        public static async Task<KeyStorage> GetMatchingMultisigAddress(string multiSigAddress, SqlexpressLykkeEntities entities)
        {
            KeyStorage ret = null;
            ret = await (from item in entities.KeyStorages
                         where item.MultiSigAddress.Equals(multiSigAddress)
                         select item).SingleOrDefaultAsync();

            if (ret == null)
            {
                throw new Exception("Could not find a matching record for MultiSigAddress: "
                    + multiSigAddress);
            }

            return ret;
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
                    ret.AssetAddress = (new BitcoinSecret(ret.AssetPrivateKey, network)).PubKey.
                        GetAddress(network);
                    ret.AssetMultiplicationFactor = item.MultiplyFactor;
                    ret.AssetDefinitionUrl = item.DefinitionUrl;
                    break;
                }
            }

            return ret;
        }

        public static async Task<Tuple<GenerateMassOutputsTaskResult, Error>> GenerateMassOutputs(TaskToDoGenerateMassOutputs data, string purpose,
            string username, string password, string ipAddress, Network network, string connectionString,
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
                        var outputs = await GetWalletOutputs(data.WalletAddress, network, entities);
                        if (outputs.Item2)
                        {
                            error = new Error();
                            error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                            error.Message = outputs.Item3;
                        }
                        else
                        {
                            var uncoloredOutputs = GetWalletOutputsUncolored(outputs.Item1);
                            float totalRequiredAmount = data.Count * data.FeeAmount * BTCToSathoshiMultiplicationFactor; // Convert to satoshi
                            float minimumRequiredAmountForParticipation = Convert.ToInt64(0.001 * BTCToSathoshiMultiplicationFactor);
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
                                    builder
                                        .AddKeys(new BitcoinSecret(data.PrivateKey))
                                        .AddCoins(sourceCoins);
                                    builder.SetChange(new BitcoinPubKeyAddress(data.WalletAddress, network));
                                    for (int i = 0; i < data.Count; i++)
                                    {
                                        builder.Send(new BitcoinPubKeyAddress(destinationAddress, network),
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
                                        Error localerror = await CheckTransactionForDoubleSpentThenSendIt
                                                    (tx, username, password, ipAddress, network, entities, connectionString,
                                                    null, (entitiesContext) =>
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
                                                            f.Network = network.ToString();
                                                            preGeneratedOutputs.Add(f);
                                                        }

                                                        entitiesContext.PreGeneratedOutputs.AddRange(preGeneratedOutputs);
                                                    });
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

        // From: http://stackoverflow.com/questions/311165/how-do-you-convert-byte-array-to-hexadecimal-string-and-vice-versa
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        // Sqlite dll is not copied to output folder, this method creates the reference, never called
        // http://stackoverflow.com/questions/14033193/entity-framework-provider-type-could-not-be-loaded
        public static void FixEfProviderServicesProblem()
        {
            var instance = System.Data.SQLite.EF6.SQLiteProviderFactory.Instance;
        }

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

        public class QBitNinjaOperation
        {
            public long amount { get; set; }
            public int confirmations { get; set; }
            public int height { get; set; }
            public string blockId { get; set; }
            public string transactionId { get; set; }
            public List<QBitNinjaReceivedCoin> receivedCoins { get; set; }
            public List<object> spentCoins { get; set; }
        }

        public class QBitNinjaUnspentOutputResponse
        {
            public object continuation { get; set; }
            public List<QBitNinjaOperation> operations { get; set; }
        }

        public class CoinprismUnspentOutput : UniversalUnspentOutput
        {
            public string transaction_hash { get; set; }
            public int output_index { get; set; }
            public long value { get; set; }
            public string asset_id { get; set; }
            public int asset_quantity { get; set; }
            public string[] addresses { get; set; }
            public string script_hex { get; set; }
            public bool spent { get; set; }
            public int confirmations { get; set; }
        }

        private class CoinprismGetBalanceResponse
        {
            public string address { get; set; }
            public string asset_address { get; set; }
            public string bitcoin_address { get; set; }
            public string issuable_asset { get; set; }
            public float balance { get; set; }
            public float unconfirmed_balance { get; set; }
            public CoinprismColoredCoinBalance[] assets { get; set; }
        }

        private class CoinprismColoredCoinBalance
        {
            public string id { get; set; }
            public string balance { get; set; }
            public string unconfirmed_balance { get; set; }
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
    }
}
