using Core;
using LykkeWalletServices;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using Core.LykkeIntegration;
using Core.LykkeIntegration.Services;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
    // Sample response CashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvCashInTask : SrvNetworkBase
    {
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public SrvCashInTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress, IPreBroadcastHandler preBroadcastHandler) : 
                base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
            _preBroadcastHandler = preBroadcastHandler;
        }

        public async Task<Tuple<CashInTaskResult, Error>> ExecuteTask(TaskToDoCashIn data)
        {
            CashInTaskResult result = null;
            Error error = null;
            try
            {
                var asset = OpenAssetsHelper.GetAssetFromName(Assets, data.Currency, connectionParams.BitcoinNetwork);

                if (asset != null)
                {
                    for (int retryCount = 0; retryCount < OpenAssetsHelper.ConcurrencyRetryCount; retryCount++)
                    {
                        try
                        {
                            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                            {
                                using (var transaction = entities.Database.BeginTransaction())
                                {
                                    PreGeneratedOutput issuancePayer = await OpenAssetsHelper.GetOnePreGeneratedOutput(entities, connectionParams,
                                        asset.AssetId);
                                    Coin issuancePayerCoin = issuancePayer.GetCoin();
                                    IssuanceCoin issueCoin = new IssuanceCoin(issuancePayerCoin);
                                    issueCoin.DefinitionUrl = new Uri(asset.AssetDefinitionUrl);

                                    // Issuing the asset
                                    TransactionBuilder builder = new TransactionBuilder();
                                    builder = builder
                                        .AddKeys(new BitcoinSecret(issuancePayer.PrivateKey, connectionParams.BitcoinNetwork))
                                        .AddCoins(issueCoin)
                                        .IssueAsset(Base58Data.GetFromBase58Data(data.MultisigAddress) as BitcoinAddress, new NBitcoin.OpenAsset.AssetMoney(
                                            new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(asset.AssetId, connectionParams.BitcoinNetwork)),
                                            Convert.ToInt64(data.Amount * asset.AssetMultiplicationFactor)));

                                    var tx = (await builder.AddEnoughPaymentFee(entities, connectionParams, FeeAddress))
                                        .BuildTransaction(true);

                                    var txHash = tx.GetHash().ToString();

                                    var handledTxRequest = new HandleTxRequest
                                    {
                                        Operation = "CashIn",
                                        TransactionId = data.TransactionId,
                                        BlockchainHash = txHash
                                    };

                                    Error localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, connectionParams, entities, ConnectionString, handledTxRequest, _preBroadcastHandler)).Error;

                                    if (localerror == null)
                                    {
                                        result = new CashInTaskResult
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
                                    break;
                                }
                            }
                        }
                        catch (DbUpdateConcurrencyException e)
                        {
                            if (retryCount == OpenAssetsHelper.ConcurrencyRetryCount - 1)
                            {
                                error = new Error();
                                error.Code = ErrorCode.PersistantConcurrencyProblem;
                                error.Message = "A concurrency problem which could not be solved: The exact error message " + e.ToString();
                            }
                        }
                    }
                }
                else
                {
                    error = new Error();
                    error.Code = ErrorCode.AssetNotFound;
                    error.Message = string.Format("Asset {0} not found.", data.Currency);
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
