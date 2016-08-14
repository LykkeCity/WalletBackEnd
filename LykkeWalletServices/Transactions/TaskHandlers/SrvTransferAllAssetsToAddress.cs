using Core;
using Core.LykkeIntegration.Services;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvTransferAllAssetsToAddressTask : SrvNetworkInvolvingExchangeBase
    {
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public SrvTransferAllAssetsToAddressTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string connectionString, IPreBroadcastHandler preBroadcastHandler) :
                base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
            _preBroadcastHandler = preBroadcastHandler;
        }

        // Sample Input: TransferAllAssetsToAddress:{"TransactionId":"10","SourceAddress":"xxx","SourcePrivateKey":"xxx","DestinationAddress":"xxx"}
        // Sample Output: TransferAllAssetsToAddress:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
        public async Task<Tuple<TransferAllAssetsToAddressTaskResult, Error>> ExecuteTask(TaskToDoTransferAllAssetsToAddress data)
        {
            TransferAllAssetsToAddressTaskResult result = null;
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
                            KeyStorage SourceMultisigAddress = null;
                            if (sourceAddress is BitcoinScriptAddress)
                            {
                                SourceMultisigAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.SourceAddress, entities);
                            }

                            var walletOutputs = await OpenAssetsHelper.GetWalletOutputs(data.SourceAddress, connectionParams.BitcoinNetwork, entities);

                            /*
                            OpenAssetsHelper.GetCoinsForWalletReturnType walletCoins = null;
                            if (sourceAddress is BitcoinPubKeyAddress)
                            {
                                walletCoins = (OpenAssetsHelper.GetOrdinaryCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, 0, 0, data.Asset,
                                    Assets, connectionParams, ConnectionString, entities, true, false);
                            }
                            else
                            {
                                walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.SourceAddress, 0, 0, data.Asset,
                                    Assets, connectionParams, ConnectionString, entities, false);
                            }
                            */
                            if (walletOutputs.Item2)
                            {
                                error = new Error();
                                error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                                error.Message = walletOutputs.Item3;
                            }
                            else
                            {
                                IList<ColoredCoin> coloredCoinList = new List<ColoredCoin>();
                                IList<Coin> unColoredCoinList = new List<Coin>();

                                

                                if (OpenAssetsHelper.apiProvider == OpenAssetsHelper.APIProvider.QBitNinja)
                                {
                                    foreach (var unspentOutput in walletOutputs.Item1)
                                    {
                                        var qbitUnspentOutput = (OpenAssetsHelper.QBitNinjaUnspentOutput)unspentOutput;
                                        if (string.IsNullOrEmpty(qbitUnspentOutput.asset_id))
                                        {
                                            Coin c = null;
                                            if (SourceMultisigAddress == null)
                                            {
                                                c = qbitUnspentOutput.GetCoinFromOutput();
                                            }
                                            else
                                            {
                                                c = new ScriptCoin(qbitUnspentOutput.GetCoinFromOutput(), new Script(SourceMultisigAddress.MultiSigScript));
                                            }
                                            unColoredCoinList.Add(c);
                                        }
                                        else
                                        {
                                            ColoredCoin cc = null;
                                            var assetMoney = new AssetMoney(new AssetId(new BitcoinAssetId(qbitUnspentOutput.asset_id)), qbitUnspentOutput.asset_quantity);
                                            if (SourceMultisigAddress == null)
                                            {
                                                Coin bearer = qbitUnspentOutput.GetCoinFromOutput();
                                                cc = new ColoredCoin(assetMoney, bearer);
                                            }
                                            else
                                            {
                                                Coin bearer = new ScriptCoin(qbitUnspentOutput.GetCoinFromOutput(), new Script(SourceMultisigAddress.MultiSigScript));
                                                cc = new ColoredCoin(assetMoney, bearer);
                                            }
                                            coloredCoinList.Add(cc);
                                        }
                                    }
                                }

                                ColoredCoin[] coloredCoins = coloredCoinList.ToArray();
                                Coin[] unColoredCoins = unColoredCoinList.ToArray();

                                using (var transaction = entities.Database.BeginTransaction())
                                {
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
                                    }
                                    else
                                    {
                                        builder.AddKeys(new BitcoinSecret(SourceMultisigAddress.WalletPrivateKey),
                                            (new BitcoinSecret(SourceMultisigAddress.WalletPrivateKey)).PubKey.GetExchangePrivateKey(entities));
                                    }

                                    IDictionary<AssetId, long> assets = new Dictionary<AssetId, long>();
                                    foreach (var coin in coloredCoins)
                                    {
                                        if (!assets.ContainsKey(coin.Amount.Id))
                                        {
                                            assets.Add(coin.Amount.Id, 0);
                                        }

                                        assets[coin.Amount.Id] += coin.Amount.Quantity;
                                    }

                                    long unColoredAmount = 0;
                                    foreach (var coin in unColoredCoins)
                                    {
                                        unColoredAmount += coin.Amount.Satoshi;
                                    }

                                    if (assets.Count > 0)
                                    {
                                        builder.AddCoins(coloredCoins);
                                        foreach (var asset in assets)
                                        {
                                            builder.SendAsset(destAddress, new AssetMoney(asset.Key, asset.Value));
                                        }
                                    }

                                    if (unColoredAmount > 0)
                                    {
                                        builder.AddCoins(unColoredCoins);
                                        builder.SendWithChange(destAddress, unColoredAmount, unColoredCoins, sourceAddress);
                                    }

                                    // assets.Count is not multiplied by 2, since we send all assets and there remains nothing
                                    builder = (await builder.AddEnoughPaymentFee(entities, connectionParams,
                                            FeeAddress, assets.Count));

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
                                        result = new TransferAllAssetsToAddressTaskResult
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
            return new Tuple<TransferAllAssetsToAddressTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoTransferAllAssetsToAddress data, Func<Tuple<TransferAllAssetsToAddressTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
