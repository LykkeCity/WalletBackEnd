using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;
using Core.LykkeIntegration;
using Core.LykkeIntegration.Services;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: Transfer:{"TransactionId":"10","SourceAddress":"mgRocLwNhXidF1Lv8sfUiNJbUXLrHehYK2","SourcePrivateKey":"???","DestinationAddress":"mt2rMXYZNUxkpHhyUhLDgMZ4Vfb1um1XvT", "Amount":2, "Asset":"TestExchangeUSD"}
    // Sample Output: Transfer:{"TransactionId":null,"Result":{"TransactionHex":"???","TransactionHash":"???"},"Error":null}
    public class SrvTransferTask : SrvNetworkInvolvingExchangeBase
    {
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public static int TransferFromPrivateWalletMinimumConfirmationNumber
        {
            get;
            set;
        }

        public static int TransferFromMultisigWalletMinimumConfirmationNumber
        {
            get;
            set;
        }

        public SrvTransferTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string connectionString, IPreBroadcastHandler preBroadcastHandler) :
                base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
            _preBroadcastHandler = preBroadcastHandler;
        }

        public async Task<Tuple<TransferTaskResult, Error>> ExecuteTask(TaskToDoTransfer data)
        {
            TransferTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var sourceAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.SourceAddress);
                    if (sourceAddress == null)
                    {
                        error = new Error();
                        error.Code = ErrorCode.InvalidAddress;
                        error.Message = "Invalid source address provided";
                    }
                    else
                    {
                        var destAddress = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.DestinationAddress);
                        if (destAddress == null)
                        {
                            error = new Error();
                            error.Code = ErrorCode.InvalidAddress;
                            error.Message = "Invalid destination address provided";
                        }
                        else
                        {
                            /*
                            KeyStorage SourceMultisigAddress = null;
                            if (sourceAddress is BitcoinScriptAddress)
                            {
                                SourceMultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.SourceAddress, entities);
                            }
                            */
                            OpenAssetsHelper.GetCoinsForWalletReturnType walletCoins = null;
                            Func<int> MinimumConfirmationNumberFunc = null;
                            if (sourceAddress is BitcoinPubKeyAddress)
                            {
                                MinimumConfirmationNumberFunc = (() => { return TransferFromPrivateWalletMinimumConfirmationNumber; });
        
                                walletCoins = (OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, data.Asset.GetAssetBTCAmount(data.Amount), data.Amount, data.Asset,
                                    Assets, connectionParams, ConnectionString, entities, true, false, MinimumConfirmationNumberFunc);
                            }
                            else
                            {
                                MinimumConfirmationNumberFunc = (() => { return TransferFromMultisigWalletMinimumConfirmationNumber; });

                                walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, data.Asset.GetAssetBTCAmount(data.Amount), data.Amount, data.Asset,
                                    Assets, connectionParams, ConnectionString, entities, false, true,  MinimumConfirmationNumberFunc);
                            }
                            if (walletCoins.Error != null)
                            {
                                error = walletCoins.Error;
                            }
                            else
                            {
                                using (var transaction = entities.Database.BeginTransaction())
                                {
                                    Coin[] uncoloredCoins = null;
                                    TransactionBuilder builder = new TransactionBuilder();
                                    builder
                                        .SetChange(sourceAddress, ChangeType.Colored);
                                    if (sourceAddress is BitcoinPubKeyAddress)
                                    {
                                        if (OpenAssetsHelper.PrivateKeyWillBeSubmitted)
                                        {
                                            builder.AddKeys(new BitcoinSecret(OpenAssetsHelper.GetPrivateKeyForAddress(data.SourceAddress)));
                                        }
                                        else
                                        {
                                            builder.AddKeys(new BitcoinSecret(data.SourcePrivateKey, connectionParams.BitcoinNetwork));
                                        }
                                        if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                        {
                                            builder.AddCoins(((OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)walletCoins).AssetCoins);
                                        }
                                        else
                                        {
                                            uncoloredCoins = ((OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)walletCoins).Coins;
                                            builder.AddCoins(uncoloredCoins);
                                        }
                                    }
                                    else
                                    {
                                        builder.AddKeys(new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey),
                                            (new BitcoinSecret(walletCoins.MatchingAddress.WalletPrivateKey)).PubKey.GetExchangePrivateKey(entities));
                                        if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                        {
                                            builder.AddCoins(((OpenAssetsHelper.GetScriptCoinsForWalletReturnType)walletCoins).AssetScriptCoins);
                                        }
                                        else
                                        {
                                            uncoloredCoins = ((OpenAssetsHelper.GetScriptCoinsForWalletReturnType)walletCoins).ScriptCoins;
                                            builder.AddCoins(uncoloredCoins);
                                        }
                                    }

                                    if (OpenAssetsHelper.IsRealAsset(data.Asset))
                                    {
                                        builder = (await builder.SendAsset(destAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                                        .AddEnoughPaymentFee(entities, connectionParams,
                                        FeeAddress, 2));
                                    }
                                    else
                                    {
                                        builder.SendWithChange(destAddress,
                                            Convert.ToInt64(data.Amount * OpenAssetsHelper.BTCToSathoshiMultiplicationFactor),
                                            uncoloredCoins,
                                            sourceAddress);
                                        builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                            FeeAddress, 0));
                                    }

                                    var tx = builder.BuildTransaction(true);

                                    var txHash = tx.GetHash().ToString();

                                    var handledTxRequest = new HandleTxRequest
                                    {
                                        Operation = "Transfer",
                                        TransactionId = data.TransactionId,
                                        BlockchainHash = txHash
                                    };

                                    Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, connectionParams, entities, ConnectionString, handledTxRequest, _preBroadcastHandler)).Error;

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
