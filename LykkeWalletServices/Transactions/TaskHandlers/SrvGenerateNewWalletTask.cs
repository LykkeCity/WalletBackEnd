using Core;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample input: GenerateNewWallet:{"TransactionId":"10"}
    // Sample output: {"TransactionId":"10","Result":{"WalletAddress":"xxxx","WalletPrivateKey":"xxxx","MultiSigAddress":"xxx"},"Error":null}
    public class SrvGenerateNewWalletTask
    {
        private Network network = null;
        private string exchangePrivateKey = null;
        public SrvGenerateNewWalletTask(Network network, string exchangePrivateKey)
        {
            this.network = network;
            this.exchangePrivateKey = exchangePrivateKey;
        }
        
        public async Task<Tuple<GenerateNewWalletTaskResult, Error>> ExecuteTask()
        {
            GenerateNewWalletTaskResult result = null;
            Error error = null;
            try
            {
                Key key = new Key();
                BitcoinSecret secret = new BitcoinSecret(key, this.network);
                result = new GenerateNewWalletTaskResult();
                result.WalletAddress = secret.GetAddress().ToWif();
                result.WalletPrivateKey = secret.PrivateKey.GetWif(network).ToWif();

                var multiSigAddress = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(2, new PubKey[] { secret.PubKey ,
                (new BitcoinSecret(exchangePrivateKey, network)).PubKey });
                result.MultiSigAddress = multiSigAddress.GetScriptAddress(Network.Main).ToString();

                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    KeyStorage item = new KeyStorage
                    {
                        WalletAddress = result.WalletAddress,
                        WalletPrivateKey = result.WalletPrivateKey,
                        MultiSigAddress = result.MultiSigAddress,
                        ExchangePrivateKey = exchangePrivateKey,
                        MultiSigScript = multiSigAddress.ToString(),
                        Network = (network == Network.Main ? NetworkType.Main : NetworkType.TestNet).ToString()
                    };

                    entitiesContext.KeyStorages.Add(item);
                    await entitiesContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<GenerateNewWalletTaskResult, Error>(result, error);
        }

        /*
        {
        "address": "1Ls5Xa9GAoUJ33L9USjgs8F1yZt4noFuzq",
        "asset_address": "akWpxmjxbeHMyuHVWX4qsWz9bKB4EnAc6AE",
        "bitcoin_address": "1Ls5Xa9GAoUJ33L9USjgs8F1yZt4noFuzq",
        "issuable_asset": "AeAaWWDkF7BsFDbb4duxnb82a42MiKqVQQ",
        "balance": 0,
        "unconfirmed_balance": 0,
        "assets": [
            {
            "id": "ARe5TkHAjAZubkBMCBomNn93m9ZV6HGFqg",
            "balance": "0",
            "unconfirmed_balance": "3500"
            }
            ]
        }
        */

        /*
        private class CoinprismGetBalanceResponse
        {
            public string address { get; set; }
            public string asset_address { get; set; }
            public string bitcoin_address { get; set; }
            public string issuable_asset { get; set; }
            public float balance { get; set; }
            public float unconfirmed_balance { get; set; }
            public ColoredCoinBalance[] assets { get; set; }
        }

        private class ColoredCoinBalance
        {
            public string id { get; set; }
            public string balance { get; set; }
            public string unconfirmed_balance { get; set; }
        }
        */

        public void Execute(TaskToDoGenerateNewWallet data, Func<Tuple<GenerateNewWalletTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask();
                await invokeResult(result);
            });
        }
    }
}
