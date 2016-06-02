using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: Transfer:{"SourceMultisigAddress":"2N5H4VU7R4s5CBsyq77HQ7Gu8ZXKDz3ZHVD","SourcePrivateKey":"???","DestinationMultisigAddress":"2N3e9ZNg6uFbVg7EwnSsaWPr6VAbnDfjkTo", "Amount":100, "Asset":"bjkUSD"}
    // Sample Output: Transfer:{"TransactionId":null,"Result":{"TransactionHex":"???","TransactionHash":"???"},"Error":null}
    public class SrvTransferTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvTransferTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string exchangePrivateKey, string connectionString) :
            base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<TransferTaskResult, Error>> ExecuteTask(TaskToDoTransfer data)
        {
            TransferTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var SourceMultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.SourceMultisigAddress, entities);
                    var DestinationMultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.DestinationMultisigAddress, entities);

                    OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(SourceMultisigAddress.MultiSigAddress, 0, data.Amount, data.Asset,
                        Assets, Network, Username, Password, IpAddress, ConnectionString, entities, false);
                    if (walletCoins.Error != null)
                    {
                        error = walletCoins.Error;
                    }
                    else
                    {
                        using (var transaction = entities.Database.BeginTransaction())
                        {
                            TransactionBuilder builder = new TransactionBuilder();
                            var tx = (await builder
                                .SetChange(new Script(SourceMultisigAddress.MultiSigScript).GetScriptAddress(Network), ChangeType.Colored)
                                .AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                                .AddCoins(walletCoins.AssetScriptCoins)
                                .SendAsset(new Script(DestinationMultisigAddress.MultiSigScript).GetScriptAddress(Network), new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, Network)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                                .AddEnoughPaymentFee(entities, Network.ToString(), FeeAddress, walletCoins.AssetScriptCoins.Length))
                                .BuildTransaction(true);

                            var txHash = tx.GetHash().ToString();

                            OpenAssetsHelper.LykkeJobsNotificationMessage lykkeJobsNotificationMessage =
                                new OpenAssetsHelper.LykkeJobsNotificationMessage();
                            lykkeJobsNotificationMessage.Operation = "Transfer";
                            lykkeJobsNotificationMessage.TransactionId = data.TransactionId;
                            lykkeJobsNotificationMessage.BlockchainHash = txHash;

                            Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                (tx, Username, Password, IpAddress, Network, entities, ConnectionString, lykkeJobsNotificationMessage);

                            if (localerror == null)
                            {
                                result = new TransferTaskResult
                                {
                                    TransactionHex = tx.ToHex(),
                                    TransactionHash = txHash
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
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<TransferTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoTransfer data, Func<Tuple<TransferTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
