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
            foreach (var item in input)
            {
                if (item.asset_id == assetId)
                {
                    outputs.Add(item);
                }
            }

            return outputs.ToArray();
        }
        public static async Task<Tuple<CoinprismUnspentOutput[], bool, string>> GetWalletOutputs(string walletAddress)
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
                    HttpResponseMessage result = await client.GetAsync(baseUrl + walletAddress + "/unspents");
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
        public static async Task<Tuple<float, float, bool, string>> GetAccountBalance(string walletAddress, string assetId)
        {
            float balance = 0;
            float unconfirmedBalance = 0;
            bool errorOccured = false;
            string errorMessage = "";
            // ToDo - We currently use coinprism api, later we should replace
            // with our self implementation
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage result = await client.GetAsync(baseUrl + walletAddress);
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

        /// <summary>
        /// Checks whether the amount for assetId of the wallet is enough
        /// </summary>
        /// <param name="walletAddress">Address of the wallet</param>
        /// <param name="assetId">Asset id to check the balance for.</param>
        /// <param name="amount">The required amount to check for.</param>
        /// <returns>Whether the asset amount is enough or not.</returns>
        /// ToDo - Figure out a method for unconfirmed balance
        public static async Task<bool> IsAssetsEnough(string walletAddress, string assetId,
            int amount, bool includeUnconfirmed = false)
        {
            Tuple<float, float, bool, string> result = await GetAccountBalance(walletAddress, assetId);
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

        // ToDo - Remove call from api.blockcypher.com
        // The returned object is a Tuple with first parameter specifing if an error has occured,
        // second the error message and third the transaction hex
        public static async Task<Tuple<bool, string, string>> GetTransactionHex(string transactionId)
        {
            string transactionHex = "";
            bool errorOccured = false;
            string errorMessage = "";
            // ToDo - We currently use coinprism api, later we should replace
            // with our self implementation
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage result = await client.GetAsync("https://api.blockcypher.com/v1/btc/main/txs/"
                        + transactionId + "?includeHex=true");
                    if (!result.IsSuccessStatusCode)
                    {
                        return new Tuple<bool, string, string>(true, result.ReasonPhrase, "");
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        BlockCypherGetTransactionResult response = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockCypherGetTransactionResult>
                            (webResponse);
                        transactionHex = response.hex;
                    }
                }
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


        /*
        [
  {
    "transaction_hash": "ee960436a29ac507b59843ba1ec7d9cfcbb6decf9645d0a7308993c2b65910a7",
    "output_index": 0,
    "value": 600,
    "asset_id": "ARe5TkHAjAZubkBMCBomNn93m9ZV6HGFqg",
    "asset_quantity": 3500,
    "addresses": [
      "1Ls5Xa9GAoUJ33L9USjgs8F1yZt4noFuzq"
    ],
    "script_hex": "76a914d9e2f78ddf74c17706d23338de6058a10c6b552e88ac",
    "spent": false,
    "confirmations": 533
  },
  {
    "transaction_hash": "81da11841c59718186bebf24f91228954363123c248961f3da26f7db686a640e",
    "output_index": 0,
    "value": 600,
    "asset_id": "ASYfetm7ue3Pk5NyK9NDdGU9mWHApaPuur",
    "asset_quantity": 250000,
    "addresses": [
      "1Ls5Xa9GAoUJ33L9USjgs8F1yZt4noFuzq"
    ],
    "script_hex": "76a914d9e2f78ddf74c17706d23338de6058a10c6b552e88ac",
    "spent": false,
    "confirmations": 0
  }
]

        */

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
