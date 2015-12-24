using Core;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
    public class SrvCashInTask
    {
        private Network network = null;
        private OpenAssetsHelper.AssetDefinition[] assets = null;
        private string username = null;
        private string password = null;
        private string ipAddress = null;
        public SrvCashInTask(Network network, OpenAssetsHelper.AssetDefinition[] assets,
            string username, string password, string ipAddress)
        {
            this.network = network;
            this.assets = assets;
            this.username = username;
            this.password = password;
            this.ipAddress = ipAddress;
        }

        public async Task<Tuple<CashInTaskResult, Error>> ExecuteTask(TaskToDoCashIn data)
        {
            CashInTaskResult result = null;
            Error error = null;
            try
            {
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    var matchingAddress = await (from item in entitiesContext.KeyStorages
                                                 where item.MultiSigAddress.Equals(data.MultisigAddress)
                                                 select item).SingleOrDefaultAsync();
                    if (matchingAddress == null)
                    {
                        throw new Exception("Could not find a matching record for MultiSigAddress: " + data.MultisigAddress);
                    }

                    string assetId = null;
                    string assetPrivateKey = null;
                    BitcoinAddress assetAddress = null;


                    // Getting the assetid from asset name
                    foreach (var item in assets)
                    {
                        if (item.Name == data.Currency)
                        {
                            assetId = item.AssetId;
                            assetPrivateKey = item.PrivateKey;
                            assetAddress = (new BitcoinSecret(assetPrivateKey, network)).PubKey.
                                GetAddress(network);
                            break;
                        }
                    }

                    var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(assetAddress.ToString(), network);
                    if (walletOutputs.Item2)
                    {
                        error = new Error();
                        error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                        error.Message = walletOutputs.Item3;

                    }
                    else
                    {
                        var bitcoinOutputs = OpenAssetsHelper.GetWalletOutputsUncolored(walletOutputs.Item1);
                        if (!OpenAssetsHelper.IsBitcoinsEnough(bitcoinOutputs, OpenAssetsHelper.MinimumRequiredSatoshi))
                        {
                            error = new Error();
                            error.Code = ErrorCode.NotEnoughBitcoinInTransaction;
                            error.Message = "The required amount of satoshis to send transaction is " + OpenAssetsHelper.MinimumRequiredSatoshi +
                                " . The address is: " + assetAddress;
                        }
                        else
                        {
                            var coins = (await OpenAssetsHelper.GetColoredUnColoredCoins(bitcoinOutputs, null, network,
                                username, password, ipAddress)).Item2;
                            IssuanceCoin issueCoin = new IssuanceCoin(coins.Last());
                            var txCoins = coins.Take(coins.Length - 1);

                            var multiSigScript = new Script(matchingAddress.MultiSigScript);
                            // Issuing the asset
                            TransactionBuilder builder = new TransactionBuilder();
                            var tx = builder
                                .AddKeys(new BitcoinSecret(assetPrivateKey))
                                .AddCoins(issueCoin)
                                .AddCoins(txCoins)
                                .IssueAsset(multiSigScript, new NBitcoin.OpenAsset.AssetMoney(new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(assetId, network)), data.Amount))
                                .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                                .SetChange(assetAddress)
                                .BuildTransaction(true);

                            RPCClient client = new RPCClient(new System.Net.NetworkCredential(username, password),
                                ipAddress, network);

                            await client.SendRawTransactionAsync(tx);

                            foreach(var item in coins)
                            {
                                // item.Outpoint.Hash
                            }

                            result = new CashInTaskResult
                            {
                                TransactionHex = tx.ToHex()
                            };
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
            return new Tuple<CashInTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoCashIn data, Func<Tuple<CashInTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
