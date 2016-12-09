using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.OpenAsset;
using System.Collections.Concurrent;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;
using System.IO;
using System.Collections;
using NBitcoin.RPC;

namespace LykkeWalletServices.BlockchainManager
{
    public class LykkeBitcoinBlockchainManager
    {
        public enum APIProvider
        {
            QBitNinja
        }

        public const APIProvider apiProvider = APIProvider.QBitNinja;

        public static async Task<string> GetTransactionHex(string transactionId, SqlexpressLykkeEntities entities = null)
        {
            string retValue = null;
            if (entities != null)
            {
                retValue = (from tx in entities.LkeBlkChnMgrTransactionsToBeSents
                            where tx.TransactionId == transactionId
                            select tx.TransactionHex).FirstOrDefault();
            }

            if (retValue == null)
            {
                return (await GetTransactionQBitNinja(transactionId))?.transaction;
            }
            else
            {
                return retValue;
            }
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

        public readonly static Func<int> DefaultGetMinimumConfirmationNumber = (() => { return MinimumNumberOfRequiredConfirmations; });

        public static async Task<QBitNinjaOutputResponse> GetAddressBalance(string walletAddress, Action<HttpResponseMessage> onNotSuccessfulReturn,
            bool colored = true, bool unspentonly = true, bool ignoreUnconfirmed = false)
        {
            string continuation = null;
            List<QBitNinjaOperation> operations = new List<QBitNinjaOperation>();

            do
            {
                QBitNinjaOutputResponse notProcessedUnspentOutputs = null;
                using (WalletBackendHTTPClient client = new WalletBackendHTTPClient())
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

        public static async Task<QBitNinjaTransactionResponse> GetTransactionQBitNinja(string transactionId,
            bool colored = false)
        {
            string url = null;
            url = string.Format("{0}?colored={1}",
                QBitNinjaTransactionUrl + transactionId, colored);

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage result = await client.GetAsync(url);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var webResponse = await result.Content.ReadAsStringAsync();
                    var response = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaTransactionResponse>
                            (webResponse);
                    return response;
                }
                else
                {
                    return null;
                }
            }
        }

        public class TransactionNode
        {
            public string TransactionId
            {
                get;
                set;
            }

            public LkeBlkChnMgrTransactionsToBeSent txInDB
            {
                get;
                set;
            }

            public List<string> Parents
            {
                get;
                set;
            }

            public List<string> Children
            {
                get;
                set;
            }
        }

        private static IList<TransactionNode> GetFlatListFromTransactions(LkeBlkChnMgrTransactionsToBeSent[] transactions)
        {
            var validTransactionList = transactions.Select(t => t.TransactionId);
            IList<TransactionNode> returnNodes = new List<TransactionNode>();

            foreach (var tx in transactions)
            {
                Transaction transaction = new Transaction(tx.TransactionHex);
                var node = new TransactionNode();
                node.TransactionId = tx.TransactionId;
                node.txInDB = tx;
                node.Parents = transaction.Inputs.Select(i => i.PrevOut.Hash.ToString()).Distinct()
                    .ToList().Join(validTransactionList, x => x, y => y, (x, y) => x).ToList();
            }

            return returnNodes;
        }

        public static async Task BroadcastAsManyTransactionsAsPossible()
        {
            try
            {
                using (SqlexpressLykkeEntities entities =
                    new SqlexpressLykkeEntities(LykkeBitcoinBlockchainManagerSettings.ConnectionString))
                {
                    var txToBroadcast = (from tx in entities.LkeBlkChnMgrTransactionsToBeSents
                                         where tx.ShouldBeSent == true && tx.HasBeenSent == false
                                         select tx).ToArray();


                    var flatList = GetFlatListFromTransactions(txToBroadcast);
                    var populatedFlatList = BuildTree(flatList);

                    Queue<KeyValuePair<string, TransactionNode>> sendingQueue = new Queue<KeyValuePair<string, TransactionNode>>();
                    foreach (var item in populatedFlatList)
                    {
                        if (item.Value.Parents.Count == 0)
                        {
                            sendingQueue.Enqueue(item);
                        }
                    }

                    KeyValuePair<string, TransactionNode> node;
                    while (sendingQueue.Count > 0)
                    {
                        node = sendingQueue.Dequeue();

                        while (true)
                        {
                            try
                            {
                                RPCClient client = new RPCClient(new System.Net.NetworkCredential(LykkeBitcoinBlockchainManagerSettings.RPCUsername, LykkeBitcoinBlockchainManagerSettings.RPCPassword),
                                    LykkeBitcoinBlockchainManagerSettings.RPCIPAddress, LykkeBitcoinBlockchainManagerSettings.Network);

                                await client.SendRawTransactionAsync(new Transaction(node.Value.txInDB.TransactionHex));

                                break;
                            }
                            catch (Exception exp)
                            {
                                // In some cases we would retry sendig
                            }
                        }

                        node.Value.txInDB.HasBeenSent = true;
                        await entities.SaveChangesAsync();

                        foreach (var childTxId in node.Value.Children)
                        {
                            populatedFlatList[childTxId].Parents.Remove(childTxId);
                            if (populatedFlatList[childTxId].Parents.Count == 0)
                            {
                                sendingQueue.Enqueue
                                    (new KeyValuePair<string, TransactionNode>(childTxId, populatedFlatList[childTxId]));
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Exception should never raise from this function, it may have unexpected result
            }
        }

        public class QBitNinjaUnspentOutputComparer : IEqualityComparer<QBitNinjaUnspentOutput>
        {
            public bool Equals(QBitNinjaUnspentOutput x, QBitNinjaUnspentOutput y)
            {
                if(x.transaction_hash == y.transaction_hash && x.output_index == y.output_index)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(QBitNinjaUnspentOutput obj)
            {
                return (obj.transaction_hash + obj.output_index.ToString()).GetHashCode();
            }

        }

        public class TransactionNodeComparer : IComparer<TransactionNode>
        {
            public int Compare(TransactionNode x, TransactionNode y)
            {
                var x1 = x.Parents.Count;
                var y1 = y.Parents.Count;

                if (x1 < y1)
                {
                    return -1;
                }
                else
                {
                    if (x1 == y1)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
        }

        public static IDictionary<string, TransactionNode> BuildTree(IEnumerable<TransactionNode> source)
        {
            IDictionary<string, TransactionNode> ret = new Dictionary<string, TransactionNode>();
            var arraySource = source.ToArray();
            for (int i = 0; i < source.Count(); i++)
            {
                var item = arraySource[i];
                var itemParents = source.Where(s => item.Parents.Contains(s.TransactionId));
                foreach (var p in itemParents)
                {
                    p.Children.Add(item.TransactionId);
                }

                ret.Add(item.txInDB.TransactionId, item);
            }

            return ret;
        }

        public static async Task<Tuple<UniversalUnspentOutput[], bool, string, bool>> GetWalletOutputs(string walletAddress,
            Network network, SqlexpressLykkeEntities entities, Func<int> getMinimumConfirmationNumber = null, bool ignoreUnconfirmed = false)
        {
            Tuple<UniversalUnspentOutput[], bool, string, bool> ret = null;
            switch (apiProvider)
            {
                case APIProvider.QBitNinja:
                    var qbitResult = await GetWalletOutputsQBitNinja(walletAddress, network, entities, getMinimumConfirmationNumber, ignoreUnconfirmed);
                    ret = new Tuple<UniversalUnspentOutput[], bool, string, bool>(qbitResult.Item1 != null ? qbitResult.Item1.Select(c => (UniversalUnspentOutput)c).ToArray() : null,
                        qbitResult.Item2, qbitResult.Item3, qbitResult.Item4);
                    break;
                default:
                    throw new Exception("Not supported.");
            }

            return ret;
        }

        private static string GetCaller(
                        [CallerFilePath] string file = "",
                        [CallerMemberName] string member = "",
                        [CallerLineNumber] int line = 0)
        {
            return string.Format("{0}_{1}({2})", Path.GetFileName(file), member, line);
        }

        public static async Task<Tuple<QBitNinjaUnspentOutput[], bool, string, bool>> GetWalletOutputsQBitNinja(string walletAddress,
            Network network, SqlexpressLykkeEntities entitiesContext, Func<int> getMinimumConfirmationNumber = null, bool ignoreUnconfirmed = false)
        {
            if (ignoreUnconfirmed)
            {
                throw new NotSupportedException("Since some transactions may not be broadcasted but outputs may be consumed by those, the support for this mode is not yet designed.");
            }

            bool errorOccured = false;
            string errorMessage = string.Empty;
            bool isInputInRace = false;
            // List is not thread safe to be runned in parallel
            ConcurrentQueue<QBitNinjaUnspentOutput> unspentOutputConcurrent = new ConcurrentQueue<QBitNinjaUnspentOutput>();
            IList<QBitNinjaUnspentOutput> blockchainUnspentOuput =
                new List<QBitNinjaUnspentOutput>();
            List<QBitNinjaUnspentOutput> mergedOutputs = new List<QBitNinjaUnspentOutput>();

            getMinimumConfirmationNumber = getMinimumConfirmationNumber ?? DefaultGetMinimumConfirmationNumber;
            var minimumConfirmationNumber = getMinimumConfirmationNumber();

            try
            {
                QBitNinjaOutputResponse notProcessedUnspentOutputs = null;

                // Getting blockchain outputs
                notProcessedUnspentOutputs = await GetAddressBalance(walletAddress,
                    (result) => { errorOccured = true; errorMessage = GetCaller() + " " + result.ToString(); }
                    , true, true, ignoreUnconfirmed);

                if (!errorOccured)
                {
                    if (notProcessedUnspentOutputs.operations != null && notProcessedUnspentOutputs.operations.Count > 0)
                    {
                        var convertResult = notProcessedUnspentOutputs.operations.Select(o => o.receivedCoins.Select(c => new QBitNinjaUnspentOutput
                        {
                            confirmations = o.confirmations,
                            output_index = c.index,
                            transaction_hash = c.transactionId,
                            value = c.value,
                            script_hex = c.scriptPubKey,
                            asset_id = c.assetId,
                            asset_quantity = c.quantity
                        }));
                        convertResult.ToList().ForEach(l => l.ToList().ForEach(uo => unspentOutputConcurrent.Enqueue(uo)));

                        // ToDo: Possible performance improbement here possible here
                        // removing outputs in blockchain which has been spent in database
                        blockchainUnspentOuput = unspentOutputConcurrent.ToList();

                        IList<int> spentItems = new List<int>();
                        for (int i = 0; i < blockchainUnspentOuput.Count; i++)
                        {
                            var uo = blockchainUnspentOuput[i];

                            var isOutputSpent = (from item in entitiesContext.LkeBlkChnMgrCoins
                                                 where item.TransactionId == uo.transaction_hash && item.OutputNumber == uo.output_index && (item.SpentTransactionId != null || item.SpentTransactionId != "")
                                                 select item).Any();
                            if (isOutputSpent)
                            {
                                spentItems.Add(i);
                            }
                        }
                        ((List<int>)spentItems).Sort();
                        spentItems = spentItems.Reverse().ToList();

                        for (int i = 0; i < spentItems.Count; i++)
                        {
                            blockchainUnspentOuput.RemoveAt(spentItems[i]);
                        }

                        // Getting database unspent outpus
                        var dbUnspentOutptus = (from item in entitiesContext.LkeBlkChnMgrCoins
                                               where item.BitcoinAddress == walletAddress && (item.SpentTransactionId == null || item.SpentTransactionId == "")
                                               select new QBitNinjaUnspentOutput { asset_id = item.AssetId, asset_quantity = item.AssetAmount ?? 0, confirmations = 0,
                                                transaction_hash = item.TransactionId, output_index = item.OutputNumber, value = item.SatoshiAmount, script_hex = item.OutputScript}).ToArray();

                        mergedOutputs = blockchainUnspentOuput.ToList();
                        mergedOutputs.AddRange(dbUnspentOutptus);

                        mergedOutputs = mergedOutputs.Where(o => o.confirmations > getMinimumConfirmationNumber()).
                            Distinct(new QBitNinjaUnspentOutputComparer()).ToList();
                    }
                    else
                    {
                        errorOccured = true;
                        errorMessage = "No coins to retrieve.";
                    }
                }
            }
            catch (Exception e)
            {
                errorOccured = true;
                errorMessage = e.ToString();
            }

            // return new Tuple<QBitNinjaUnspentOutput[], bool, string>(unspentOutputsList.ToArray(), errorOccured, errorMessage);
            return new Tuple<QBitNinjaUnspentOutput[], bool, string, bool>(mergedOutputs.ToArray(),
                errorOccured, errorMessage, isInputInRace);
        }

        public static async Task<bool> HasTransactionPassedItsWaitTime(string txId,
            SqlexpressLykkeEntities entitiesContext)
        {
            if (entitiesContext == null)
            {
                return true;
            }

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

        public static async Task<BroadcastResult> BroadcastTransaction(string transactionHex, string reference = null)
        {
            try
            {
                using (var entites =
                    new SqlexpressLykkeEntities(LykkeBitcoinBlockchainManagerSettings.ConnectionString))
                {
                    IList<Coin> inputCoins = new List<Coin>();
                    IList<ColoredCoin> inputColoredCoins = new List<ColoredCoin>();

                    Transaction transaction = new Transaction(transactionHex);
                    Coin issuanceCoin = null;
                    for (int i = 0; i < transaction.Inputs.Count; i++)
                    {
                        var item = transaction.Inputs[i];
                        var txIdToFind = item.PrevOut.Hash.ToString();
                        var blockchainManagerCoin = (from bmc in entites.LkeBlkChnMgrCoins
                                                     where bmc.TransactionId == txIdToFind && bmc.OutputNumber == item.PrevOut.N
                                                     select bmc).FirstOrDefault();

                        Coin bearer = null;
                        if (blockchainManagerCoin != null)
                        {
                            var txHex = await GetTransactionHex(blockchainManagerCoin.TransactionId);
                            bearer = new Coin(new Transaction(txHex), (uint)blockchainManagerCoin.OutputNumber);
                            if (string.IsNullOrEmpty(blockchainManagerCoin.AssetId))
                            {
                                inputCoins.Add(bearer);
                            }
                            else
                            {
                                ColoredCoin coin = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(blockchainManagerCoin.AssetId)), blockchainManagerCoin.AssetAmount ?? 0), bearer);
                                inputColoredCoins.Add(coin);
                            }
                        }
                        else
                        {
                            // It should be in the blockchain
                            var blockchainTransaction = await GetTransactionQBitNinja(item.PrevOut.Hash.ToString(), true);
                            foreach (var output in blockchainTransaction.receivedCoins)
                            {
                                if (output.index == item.PrevOut.N)
                                {
                                    bearer = new Coin(new Transaction(blockchainTransaction.transaction), (uint)output.index);
                                    if (output.quantity == 0)
                                    {
                                        inputCoins.Add(bearer);
                                    }
                                    else
                                    {
                                        ColoredCoin coin = new ColoredCoin(new AssetMoney(new AssetId(new BitcoinAssetId(output.assetId)),
                                            output.quantity), bearer);
                                        inputColoredCoins.Add(coin);
                                    }

                                    break;
                                }
                            }
                        }

                        if (i == 0)
                        {
                            issuanceCoin = bearer;
                        }
                    }

                    TransactionBuilder builder = new TransactionBuilder();
                    builder.AddCoins(inputCoins);
                    builder.AddCoins(inputColoredCoins);
                    var verified = builder.Verify(transaction);

                    if (!verified)
                    {
                        return new BroadcastResult { Error = BroadcastError.TransactionVerificationError, BroadcastException = null };
                    }

                    entites.LkeBlkChnMgrTransactionsToBeSents.Add(new LkeBlkChnMgrTransactionsToBeSent
                    {
                        TransactionId = transaction.GetHash().ToString(),
                        TransactionHex = transaction.ToHex(),
                        CreationDate = DateTime.UtcNow,
                        ShouldBeSent = true,
                        ReferenceNumber = reference
                    });

                    var colorMarkerIndex = -1;
                    for (int i = 0; i < transaction.Outputs.Count; i++)
                    {
                        var item = transaction.Outputs[i];
                        if (item.ScriptPubKey.ToHex().ToLower().StartsWith("6a"))
                        {
                            colorMarkerIndex = i;
                            break;
                        }
                    }

                    var txId = transaction.GetHash().ToString();
                    var colorMarker = transaction.GetColoredMarker();

                    LkeBlkChnMgrCoin[] coinsToAddToDB = new LkeBlkChnMgrCoin[transaction.Outputs.Count];
                    for (int i = 0; i < transaction.Outputs.Count; i++)
                    {
                        var item = transaction.Outputs[i];
                        coinsToAddToDB[i] = new LkeBlkChnMgrCoin
                        {
                            TransactionId = txId,
                            OutputNumber = i,
                            SatoshiAmount = item.Value,
                            AssetId = null,
                            AssetAmount = 0
                        };
                    }

                    if (colorMarkerIndex > -1)
                    {
                        ColoredTransaction coloredTx = new ColoredTransaction(transaction, inputColoredCoins.ToArray(),
                            issuanceCoin.ScriptPubKey);

                        for (int i = 0; i < transaction.Outputs.Count; i++)
                        {
                            var item = transaction.Outputs[i];
                            if (i < colorMarkerIndex)
                            {
                                // This is an issuance
                                if (i < coloredTx.Issuances.Count)
                                {
                                    var assetId = coloredTx.Issuances[i].Asset.Id.GetWif(LykkeBitcoinBlockchainManagerSettings.Network).ToString();
                                    var assetQuantity = coloredTx.Issuances[i].Asset.Quantity;

                                    if (assetQuantity > 0)
                                    {
                                        coinsToAddToDB[i].AssetId = assetId;
                                        coinsToAddToDB[i].AssetAmount = assetQuantity;
                                    }
                                    else
                                    {
                                        // Output is uncolored
                                        continue;
                                    }
                                }
                                else
                                {
                                    // Output is not listed
                                    continue;
                                }
                            }
                            else
                            {
                                if (i == colorMarkerIndex)
                                {
                                    // This is the marker output
                                    continue;
                                }
                                else
                                {
                                    var index = i - (colorMarkerIndex + 1);
                                    // This asset transfer output
                                    if (index < coloredTx.Transfers.Count)
                                    {
                                        var assetId = coloredTx.Transfers[index].Asset.Id.GetWif(LykkeBitcoinBlockchainManagerSettings.Network).ToString();
                                        var assetQuantity = coloredTx.Transfers[index].Asset.Quantity;

                                        if (assetQuantity > 0)
                                        {
                                            coinsToAddToDB[i].AssetId = assetId;
                                            coinsToAddToDB[i].AssetAmount = assetQuantity;
                                        }
                                        else
                                        {
                                            // Output is uncolored
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        // Output is not listed
                                        continue;
                                    }
                                }
                            }
                        }
                    }

                    entites.LkeBlkChnMgrCoins.AddRange(coinsToAddToDB);
                    await entites.SaveChangesAsync();

                    for (int i = 0; i < transaction.Inputs.Count; i++)
                    {
                        var item = transaction.Inputs[i];

                        var matchedTxId = item.PrevOut.Hash.ToString();
                        var dbCoin = (from c in entites.LkeBlkChnMgrCoins
                                      where c.TransactionId == matchedTxId && c.OutputNumber == item.PrevOut.N
                                      select c).FirstOrDefault();

                        if (dbCoin != null)
                        {
                            dbCoin.SpentTransactionId = txId;
                            dbCoin.SpentTransactionInputNumber = i;
                            await entites.SaveChangesAsync();
                        }
                    }
                }

                return new BroadcastResult { Error = BroadcastError.GeneralError, BroadcastException = null };
            }
            catch (Exception e)
            {
                return new BroadcastResult { Error = BroadcastError.GeneralError, BroadcastException = e };
            }
        }
    }

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
    #endregion
}
