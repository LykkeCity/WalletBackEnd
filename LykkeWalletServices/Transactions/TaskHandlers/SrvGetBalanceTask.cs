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
    /// <summary>
    /// The task which handles the Getting of the account balance
    /// The sample in Azure inqueue could be: GetBalance:{"TransactionId":"10", "WalletAddress":"1Ls5Xa9GAoUJ33L9USjgs8F1yZt4noFuzq", "AssetID":"ARe5TkHAjAZubkBMCBomNn93m9ZV6HGFqg"}
    /// For the above input the output would be something similar to: Core.TransactionResultModel:{"TransactionId":"10","Result":{"Balance":3500.0,"UnconfirmedBalance":0.0,"HasErrorOccurred":false,"ErrorMessage":null}}
    /// </summary>
    public class SrvGetBalanceTask
    {
        private Network network;
        public SrvGetBalanceTask(Network network)
        {
            this.network = network;
        }
        public async Task<TaskResultGetBalance> ExecuteTask(TaskToDoGetBalance data)
        {
            TaskResultGetBalance resultGetBalance = new TaskResultGetBalance();
            var ret = await OpenAssetsHelper.GetAccountBalance(data.WalletAddress, data.AssetID, network);
            resultGetBalance.Balance = ret.Item1;
            resultGetBalance.HasErrorOccurred = ret.Item2;
            resultGetBalance.ErrorMessage = ret.Item3;
            resultGetBalance.SequenceNumber = -1;
            return resultGetBalance;
            /*
            // ToDo - We currently use coinprism api, later we should replace
            // with our self implementation
            string baseUrl = "https://api.coinprism.com/v1/addresses/";
            TaskResultGetBalance resultGetBalance = new TaskResultGetBalance();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage result = await client.GetAsync(baseUrl + data.WalletAddress);
                    if (!result.IsSuccessStatusCode)
                    {
                        resultGetBalance.HasErrorOccurred = true;
                        resultGetBalance.ErrorMessage = result.ReasonPhrase;
                    }
                    else
                    {
                        var webResponse = await result.Content.ReadAsStringAsync();
                        CoinprismGetBalanceResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<CoinprismGetBalanceResponse>
                            (webResponse);
                        foreach(var item in response.assets)
                        {
                            if(item.id.Equals(data.AssetID))
                            {
                                resultGetBalance.Balance = float.Parse(item.balance);
                                resultGetBalance.UnconfirmedBalance = float.Parse(item.unconfirmed_balance);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                resultGetBalance.HasErrorOccurred = true;
                resultGetBalance.ErrorMessage = e.ToString();
            }
            return resultGetBalance;
            */
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

        public void Execute(TaskToDoGetBalance data, Func<TaskResultGetBalance, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
