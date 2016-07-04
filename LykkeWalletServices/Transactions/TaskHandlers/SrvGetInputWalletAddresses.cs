using Core;
using NBitcoin;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using static LykkeWalletServices.OpenAssetsHelper;
using System.Net.Http;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: GetInputWalletAddresses:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Asset":"TestExchangeUSD"}
    // Sample response: GetInputWalletAddresses:{"TransactionId":"10","Result":{"Addresses":["mhF3ghWGgAJxDcUC52ar5DCv56MVzpN94W","2Msbkk8AGbzrVnENqE3m8nk4n6FyTrdnNF4","2MyZey5YzZMnbuzfi3RuNqnkKAuMgwzRYRj"]},"Error":null}
    public class SrvGetInputWalletAddressesTask : SrvNetworkBase
    {
        public SrvGetInputWalletAddressesTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        // This method just supports QBit.Ninja
        public async Task<Tuple<GetInputWalletAddressesTaskResult, Error>> ExecuteTask
            (TaskToDoGetInputWalletAddresses data)
        {
            GetInputWalletAddressesTaskResult result = null;
            List<string> repeatedAddress = new List<string>();

            Error error = null;
            try
            {
                string assetId = null;

                if (!(data.Asset?.ToUpper()?.Equals("BTC") ?? false))
                {
                    assetId = GetAssetFromName(Assets, data.Asset, Network)?.AssetId;
                    if (assetId == null)
                    {
                        error = new Error();
                        error.Code = ErrorCode.AssetNotFound;
                        error.Message = "Invalid asset specified.";
                    }
                }

                if (error == null)
                {
                    try
                    {
                        var validation = BitcoinAddress.Create(data.MultisigAddress, Network);
                    }
                    catch (Exception)
                    {
                        error = new Error();
                        error.Code = ErrorCode.InvalidAddress;
                        error.Message = string.Format("{0} is not a valid address for network: {1}", data.MultisigAddress, Network.ToString());
                    }

                    if (error == null)
                    {
                        var ops = await GetQBitNinjaOperation(data.MultisigAddress, null);
                        if (ops.Item1 == null)
                        {
                            foreach (var operation in ops.Item2)
                            {
                                var transaction = await GetQBitNinjaOperation(null, operation.transactionId);
                                if (transaction.Item1 == null)
                                {
                                    IList<string> rcvCoin = new List<string>();
                                    IList<string> sndCoin = new List<string>();
                                    transaction.Item2[0].receivedCoins.Where(c => (c.assetId == assetId)).ToList().ForEach(c => rcvCoin.Add(GetAddressFromScriptPubKey(new Script(StringToByteArray(c.scriptPubKey)), Network)));
                                    transaction.Item2[0].spentCoins.Where(c => (c.assetId == assetId)).ToList().ForEach(c => sndCoin.Add(GetAddressFromScriptPubKey(new Script(StringToByteArray(c.scriptPubKey)), Network)));
                                    if (rcvCoin.Contains(data.MultisigAddress))
                                    {
                                        repeatedAddress.AddRange(sndCoin);
                                    }
                                    if (sndCoin.Contains(data.MultisigAddress))
                                    {
                                        repeatedAddress.AddRange(rcvCoin);
                                    }
                                }
                                else
                                {
                                    error = ops.Item1;
                                }
                            }

                            result = new GetInputWalletAddressesTaskResult();
                            var distinctAddresses = repeatedAddress.Distinct();
                            if (distinctAddresses.Contains(data.MultisigAddress))
                            {
                                distinctAddresses = distinctAddresses.Where(c => (c != data.MultisigAddress && c != null));
                            }
                            result.Addresses = distinctAddresses.ToArray();
                        }
                        else
                        {
                            error = ops.Item1;
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

            return new Tuple<GetInputWalletAddressesTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGetInputWalletAddresses data, Func<Tuple<GetInputWalletAddressesTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }

        private async Task<Tuple<Error, QBitNinjaOperation[]>> GetQBitNinjaOperation(string multisigAddress, string transactionId)
        {
            if ((!string.IsNullOrEmpty(multisigAddress) && !string.IsNullOrEmpty(transactionId))
                || (string.IsNullOrEmpty(multisigAddress) && string.IsNullOrEmpty(transactionId)))
            {
                throw new Exception("Exactly one of multisig address or transaction id should be non null or empty.");
            }

            Error error = null;
            QBitNinjaOperation[] ops = null;
            bool isWalletOperation = true;

            using (HttpClient client = new HttpClient())
            {
                string url = null;
                if (!string.IsNullOrEmpty(multisigAddress))
                {
                    url = QBitNinjaBalanceUrl + multisigAddress;
                    isWalletOperation = true;
                }
                else
                {
                    url = QBitNinjaTransactionUrl + transactionId;
                    isWalletOperation = false;
                }
                HttpResponseMessage httpResult = await client.GetAsync(url + "?colored=true");
                if (!httpResult.IsSuccessStatusCode)
                {
                    error = new Error();
                    error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                    error.Message = httpResult.ReasonPhrase;
                }
                else
                {
                    var webResponse = await httpResult.Content.ReadAsStringAsync();
                    if (isWalletOperation)
                    {
                        var notProcessedOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaOutputResponse>
                            (webResponse);
                        if (notProcessedOutputs.operations != null &&
                            notProcessedOutputs.operations.Count > 0)
                        {
                            ops = notProcessedOutputs.operations.ToArray();
                        }
                        else
                        {
                            error = new Error();
                            error.Code = ErrorCode.NoCoinsFound;
                            error.Message = "No coins to retrieve.";
                        }
                    }
                    else
                    {
                        var notProcessedOutputs = Newtonsoft.Json.JsonConvert.DeserializeObject<QBitNinjaTransactionResponse>
                            (webResponse);
                        ops = new QBitNinjaOperation[] { new QBitNinjaOperation { blockId = notProcessedOutputs?.block?.blockId ,
                            confirmations = notProcessedOutputs?.block.confirmations ?? 0 ,
                            height = notProcessedOutputs?.block.height ?? -1,
                            receivedCoins = notProcessedOutputs?.receivedCoins ,
                            spentCoins = notProcessedOutputs?.spentCoins ,
                            transactionId = notProcessedOutputs?.transactionId  } };
                    }
                }
            }

            return new Tuple<Error, QBitNinjaOperation[]>(error, ops);
        }
    }
}
