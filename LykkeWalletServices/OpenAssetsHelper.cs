using NBitcoin;
using NBitcoin.OpenAsset;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public sealed class OpenAssetsHelper
    {
        const string baseUrl = "https://api.coinprism.com/v1/addresses/";
        const string testnetBaseUrl = "https://testnet.api.coinprism.com/v1/addresses/";
        public const uint MinimumRequiredSatoshi = 50000; // 100000000 satoshi is one BTC
        public const uint TransactionSendFeesInSatoshi = 10000;

        public class AssetDefinition
        {
            public string AssetId { get; set; }
            public string Name { get; set; }
            public string PrivateKey { get; set; }
        }

        public static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(OpenAssetsHelper.CoinprismUnspentOutput[] walletOutputs,
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

        private static ColoredCoin[] GenerateWalletColoredCoins(Transaction[] transactions, OpenAssetsHelper.CoinprismUnspentOutput[] usableOutputs, string assetId)
        {
            ColoredCoin[] coins = new ColoredCoin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), (int)usableOutputs[i].asset_quantity),
                    new Coin(transactions[i], (uint)usableOutputs[i].output_index));
            }
            return coins;
        }

        private static Coin[] GenerateWalletUnColoredCoins(Transaction[] transactions, OpenAssetsHelper.CoinprismUnspentOutput[] usableOutputs)
        {
            Coin[] coins = new Coin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new Coin(transactions[i], (uint)usableOutputs[i].output_index);
            }
            return coins;
        }

        private static async Task<Transaction[]> GetTransactionsHex(CoinprismUnspentOutput[] outputList, Network network,
            string username, string password, string ipAddress)
        {
            Transaction[] walletTransactions = new Transaction[outputList.Length];
            for (int i = 0; i < walletTransactions.Length; i++)
            {
                var ret = await GetTransactionHex(outputList[i].transaction_hash, network, username, password, ipAddress);
                if (!ret.Item1)
                {
                    walletTransactions[i] = new Transaction(ret.Item3);
                }
                else
                {
                    throw new Exception("Could not get the transaction hex for the transaction with id: "
                        + outputList[i].transaction_hash + " . The exact error message is " + ret.Item2);
                }
            }
            return walletTransactions;
        }

        // public static GetSumOfUncoloredCoins()

        public static CoinprismUnspentOutput[] GetWalletOutputsUncolored(CoinprismUnspentOutput[] input)
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
        public static async Task<Tuple<CoinprismUnspentOutput[], bool, string>> GetWalletOutputs(string walletAddress,
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
                        url = baseUrl + walletAddress;
                    }
                    else
                    {
                        url = testnetBaseUrl + walletAddress;
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

        /// <summary>
        /// Gets the asset balance for the wallet address
        /// </summary>
        /// <param name="walletAddress">The address of the wallet to check the balance for.</param>
        /// <param name="assetId">The id of asset to check the balance.</param>
        /// <returns>A tuple, first part is balance, second part is unconfirmed balance, third part is whether error has occured or not,
        ///  forth part is the error message.</returns>
        public static async Task<Tuple<float, float, bool, string>> GetAccountBalance(string walletAddress,
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
                        url = baseUrl + walletAddress;
                    }
                    else
                    {
                        url = testnetBaseUrl + walletAddress;
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

        // ToDo - Clear fractional currencies case
        // ToDo - Clear confirmation number
        public static bool IsAssetsEnough(CoinprismUnspentOutput[] outputs,
            string assetId, float assetAmount, bool includeUnconfirmed = false)
        {
            float total = 0;
            foreach (var item in outputs)
            {
                if (item.asset_id.Equals(assetId))
                {
                    if (item.confirmations == 0)
                    {
                        if (includeUnconfirmed)
                        {
                            total += (float)item.asset_quantity;
                        }
                    }
                    else
                    {
                        total += (float)item.asset_quantity;
                    }
                }
            }

            if (total >= assetAmount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        // ToDo - Clear confirmation number
        public static bool IsBitcoinsEnough(CoinprismUnspentOutput[] outputs,
            uint amountInSatoshi, bool includeUnconfirmed = false)
        {
            int total = 0;
            foreach (var item in outputs)
            {
                if (item.confirmations == 0)
                {
                    if (includeUnconfirmed)
                    {
                        total += item.value;
                    }
                }
                else
                {
                    total += item.value;
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
            int amount, Network network, bool includeUnconfirmed = false)
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


        // Sqlite dll is not copied to output folder, this method creates the reference, never called
        // http://stackoverflow.com/questions/14033193/entity-framework-provider-type-could-not-be-loaded
        public static void FixEfProviderServicesProblem()
        {
            var instance = System.Data.SQLite.EF6.SQLiteProviderFactory.Instance;
        }

        public class CoinprismUnspentOutput
        {
            public string transaction_hash { get; set; }
            public int output_index { get; set; }
            public int value { get; set; }
            public string asset_id { get; set; }
            public float? asset_quantity { get; set; }
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
