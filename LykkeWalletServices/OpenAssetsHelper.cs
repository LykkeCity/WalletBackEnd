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
    public sealed class OpenAssetsHelper
    {
        const string baseUrl = "https://api.coinprism.com/v1/addresses/";
        const string testnetBaseUrl = "https://testnet.api.coinprism.com/v1/addresses/";
        public const uint MinimumRequiredSatoshi = 50000; // 100000000 satoshi is one BTC
        public const uint TransactionSendFeesInSatoshi = 15000;

        public class AssetDefinition
        {
            public string AssetId { get; set; }
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

        public static async Task<Tuple<ColoredCoin[], Coin[]>> GetColoredUnColoredCoins(CoinprismUnspentOutput[] walletOutputs,
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

        private static ColoredCoin[] GenerateWalletColoredCoins(Transaction[] transactions, CoinprismUnspentOutput[] usableOutputs, string assetId)
        {
            ColoredCoin[] coins = new ColoredCoin[transactions.Length];
            for (int i = 0; i < transactions.Length; i++)
            {
                coins[i] = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(assetId)), (int)usableOutputs[i].asset_quantity),
                    new Coin(transactions[i], (uint)usableOutputs[i].output_index));
            }
            return coins;
        }

        private static Coin[] GenerateWalletUnColoredCoins(Transaction[] transactions, CoinprismUnspentOutput[] usableOutputs)
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

        // ToDo - Clear confirmation number
        public static float GetAssetBalance(CoinprismUnspentOutput[] outputs,
            string assetId, long multiplyFactor, bool includeUnconfirmed = false)
        {
            float total = 0;
            foreach (var item in outputs)
            {
                if (item.asset_id != null)
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
            }

            return total / multiplyFactor;
        }

        public static bool IsAssetsEnough(CoinprismUnspentOutput[] outputs,
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
            string username, string password, string ipAddress, Network network, string connectionString)
        {
            Error error = null;
            using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities(connectionString))
            {
                // Checking if the inputs has been already spent
                // ToDo - Performance should be revisted by possible join operation
                foreach (var item in tx.Inputs)
                {
                    string prevOut = item.PrevOut.Hash.ToString();
                    var spentTx = await (from uxto in entitiesContext.SpentOutputs
                                         join dbTx in entitiesContext.SentTransactions on uxto.SentTransactionId equals dbTx.id
                                         where uxto.PrevHash.Equals(prevOut) && uxto.OutputNumber.Equals(item.PrevOut.N)
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
                    // First broadcating the transaction
                    RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password),
                        ipAddress, network);
                    await client.SendRawTransactionAsync(tx);

                    // Then marking the inputs as spent
                    using (var dbTransaction = entitiesContext.Database.BeginTransaction())
                    {
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
                                OutputNumber = item.PrevOut.N,
                                PrevHash = item.PrevOut.Hash.ToString(),
                                SentTransactionId = dbSentTransaction.id
                            });
                        }
                        await entitiesContext.SaveChangesAsync();

                        dbTransaction.Commit();
                    }
                }
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

        /*
        public static async Task<GetOrdinaryCoinsForWalletReturnType> GetOrdinaryCoinsForWallet
            (string multiSigAddress, float amount, string asset, AssetDefinition[] assets,
            Network network, string username, string password, string ipAddress)
        {
            GetOrdinaryCoinsForWalletReturnType ret = new GetOrdinaryCoinsForWalletReturnType();

            try
            {
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    ret.MatchingAddress = await (from item in entitiesContext.KeyStorages
                                                 where item.MultiSigAddress.Equals(multiSigAddress)
                                                 select item).SingleOrDefaultAsync();
                }

                if (ret.MatchingAddress == null)
                {
                    throw new Exception("Could not find a matching record for MultiSigAddress: "
                        + multiSigAddress);
                }

                string assetPrivateKey = null;

                // Getting the assetid from asset name
                foreach (var item in assets)
                {
                    if (item.Name == asset)
                    {
                        ret.AssetId = item.AssetId;
                        assetPrivateKey = item.PrivateKey;
                        ret.AssetAddress = (new BitcoinSecret(assetPrivateKey, network)).PubKey.
                            GetAddress(network);
                        ret.AssetMultiplicationFactor = item.MultiplyFactor;
                        break;
                    }
                }

                // Getting wallet outputs
                var walletOutputs = await GetWalletOutputs
                    (ret.MatchingAddress.WalletAddress, network);
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
                    if (!IsBitcoinsEnough(bitcoinOutputs, MinimumRequiredSatoshi))
                    {
                        ret.Error = new Error();
                        ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                        ret.Error.Message = "The required amount of satoshis to send transaction is " + MinimumRequiredSatoshi +
                            " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                    }
                    else
                    {
                        // Getting the asset output to provide the assets
                        var assetOutputs = GetWalletOutputsForAsset(walletOutputs.Item1, ret.AssetId);
                        if (!IsAssetsEnough(assetOutputs, ret.AssetId, amount, ret.AssetMultiplicationFactor))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                            ret.Error.Message = "The required amount of assets with id:" + ret.AssetId + " to send transaction is " + amount +
                                " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                        }
                        else
                        {
                            ret.Coins = (await GetColoredUnColoredCoins(bitcoinOutputs, null, network,
                                username, password, ipAddress)).Item2;


                            ret.AssetCoins = (await GetColoredUnColoredCoins(assetOutputs, ret.AssetId, network,
                            username, password, ipAddress)).Item1;
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

        public static async Task<GetScriptCoinsForWalletReturnType> GetScriptCoinsForWallet
            (string multiSigAddress, float amount, string asset, AssetDefinition[] assets,
            Network network, string username, string password, string ipAddress)
        {
            GetScriptCoinsForWalletReturnType ret = new GetScriptCoinsForWalletReturnType();

            try
            {
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    ret.MatchingAddress = await (from item in entitiesContext.KeyStorages
                                                 where item.MultiSigAddress.Equals(multiSigAddress)
                                                 select item).SingleOrDefaultAsync();
                }

                if (ret.MatchingAddress == null)
                {
                    throw new Exception("Could not find a matching record for MultiSigAddress: "
                        + multiSigAddress);
                }

                string assetPrivateKey = null;

                // Getting the assetid from asset name
                foreach (var item in assets)
                {
                    if (item.Name == asset)
                    {
                        ret.AssetId = item.AssetId;
                        assetPrivateKey = item.PrivateKey;
                        ret.AssetAddress = (new BitcoinSecret(assetPrivateKey, network)).PubKey.
                            GetAddress(network);
                        ret.AssetMultiplicationFactor = item.MultiplyFactor;
                        break;
                    }
                }

                // Getting wallet outputs
                var walletOutputs = await GetWalletOutputs
                    (ret.MatchingAddress.MultiSigAddress, network);
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
                    if (!IsBitcoinsEnough(bitcoinOutputs, MinimumRequiredSatoshi))
                    {
                        ret.Error = new Error();
                        ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                        ret.Error.Message = "The required amount of satoshis to send transaction is " + MinimumRequiredSatoshi +
                            " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                    }
                    else
                    {
                        // Getting the asset output to provide the assets
                        var assetOutputs = GetWalletOutputsForAsset(walletOutputs.Item1, ret.AssetId);
                        if (!IsAssetsEnough(assetOutputs, ret.AssetId, amount, ret.AssetMultiplicationFactor))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                            ret.Error.Message = "The required amount of assets with id:" + ret.AssetId + " to send transaction is " + amount +
                                " . The address is: " + ret.MatchingAddress.MultiSigAddress;
                        }
                        else
                        {
                            // Converting bitcoins to script coins so that we could sign the transaction
                            var coins = (await GetColoredUnColoredCoins(bitcoinOutputs, null, network,
                                username, password, ipAddress)).Item2;
                            if (coins.Length != 0)
                            {
                                ret.ScriptCoins = new ScriptCoin[coins.Length];
                                for (int i = 0; i < coins.Length; i++)
                                {
                                    ret.ScriptCoins[i] = new ScriptCoin(coins[i], new Script(ret.MatchingAddress.MultiSigScript));
                                }
                            }
                            // Converting assets to script coins so that we could sign the transaction
                            var assetCoins = (await GetColoredUnColoredCoins(assetOutputs, ret.AssetId, network,
                            username, password, ipAddress)).Item1;

                            if (assetCoins.Length != 0)
                            {
                                ret.AssetScriptCoins = new ColoredCoin[assetCoins.Length];
                                for (int i = 0; i < assetCoins.Length; i++)
                                {
                                    ret.AssetScriptCoins[i] = new ColoredCoin(assetCoins[i].Amount,
                                        new ScriptCoin(assetCoins[i].Bearer, new Script(ret.MatchingAddress.MultiSigScript)));
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
        */

        public static async Task<GetCoinsForWalletReturnType> GetCoinsForWallet
            (string multiSigAddress, float amount, string asset, AssetDefinition[] assets,
            Network network, string username, string password, string ipAddress, string connectionString, bool isOrdinaryReturnTypeRequired, bool isAddressMultiSig = true)
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
                    ret.MatchingAddress = await GetMatchingMultisigAddress(multiSigAddress, connectionString);
                }

                ret.Asset = GetAssetFromName(assets, asset, network);
                if (ret.Asset == null)
                {
                    ret.Error = new Error();
                    ret.Error.Code = ErrorCode.AssetNotFound;
                    ret.Error.Message = "Could not find asset with name: " + asset;
                }
                else
                {
                    // Getting wallet outputs
                    var walletOutputs = await GetWalletOutputs
                        (multiSigAddress, network);
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
                        if (!IsBitcoinsEnough(bitcoinOutputs, MinimumRequiredSatoshi))
                        {
                            ret.Error = new Error();
                            ret.Error.Code = ErrorCode.NotEnoughBitcoinAvailable;
                            ret.Error.Message = "The required amount of satoshis to send transaction is " + MinimumRequiredSatoshi +
                                " . The address is: " + multiSigAddress;
                        }
                        else
                        {
                            // Getting the asset output to provide the assets
                            var assetOutputs = GetWalletOutputsForAsset(walletOutputs.Item1, ret.Asset.AssetId);
                            if (!IsAssetsEnough(assetOutputs, ret.Asset.AssetId, amount, ret.Asset.AssetMultiplicationFactor))
                            {
                                ret.Error = new Error();
                                ret.Error.Code = ErrorCode.NotEnoughAssetAvailable;
                                ret.Error.Message = "The required amount of " + asset + " to send transaction is " + amount +
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
                                // Converting assets to script coins so that we could sign the transaction
                                var assetCoins = (await GetColoredUnColoredCoins(assetOutputs, ret.Asset.AssetId, network,
                                username, password, ipAddress)).Item1;

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

        public static async Task<KeyStorage> GetMatchingMultisigAddress(string multiSigAddress, string connectionString)
        {
            KeyStorage ret = null;
            using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities(connectionString))
            {
                ret = await (from item in entitiesContext.KeyStorages
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
