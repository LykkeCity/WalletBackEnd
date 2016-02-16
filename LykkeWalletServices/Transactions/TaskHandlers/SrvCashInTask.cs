using Core;
using LykkeWalletServices;
using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: CashIn:{"TransactionId":"10","MultisigAddress":"3NQ6FF3n8jPFyPewMqzi2qYp8Y4p3UEz9B","Amount":5000,"Currency":"bjkUSD"}
    // Sample response CashIn:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    public class SrvCashInTask : SrvNetworkBase
    {
        public SrvCashInTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string connectionString, string feeAddress) : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
        }

        public async Task<Tuple<CashInTaskResult, Error>> ExecuteTask(TaskToDoCashIn data)
        {
            CashInTaskResult result = null;
            Error error = null;
            try
            {
                var asset = OpenAssetsHelper.GetAssetFromName(Assets, data.Currency, Network);

                for (int retryCount = 0; retryCount < OpenAssetsHelper.ConcurrencyRetryCount; retryCount++)
                {
                    try
                    {
                        using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                PreGeneratedOutput issuancePayer = await OpenAssetsHelper.GetOnePreGeneratedOutput(entities, Network.ToString(), asset.AssetId);
                                Coin issuancePayerCoin = issuancePayer.GetCoin();
                                IssuanceCoin issueCoin = new IssuanceCoin(issuancePayerCoin);
                                issueCoin.DefinitionUrl = new Uri(asset.AssetDefinitionUrl);

                                var multiSigScript = new Script((await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities)).MultiSigScript);
                                // Issuing the asset
                                TransactionBuilder builder = new TransactionBuilder();
                                var tx = (await builder
                                    .AddKeys(new BitcoinSecret(issuancePayer.PrivateKey, Network))
                                    .AddCoins(issueCoin)
                                    .AddEnoughPaymentFee(entities, Network.ToString()))
                                    .IssueAsset(multiSigScript.GetScriptAddress(Network), new NBitcoin.OpenAsset.AssetMoney(
                                        new NBitcoin.OpenAsset.AssetId(new NBitcoin.OpenAsset.BitcoinAssetId(asset.AssetId, Network)),
                                        Convert.ToInt64(data.Amount * asset.AssetMultiplicationFactor)))
                                    .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                                    .SetChange(new BitcoinAddress(FeeAddress, Network)) // Paying the rest to fee payer address
                                    .BuildTransaction(true);

                                Error localerror = await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                    (tx, Username, Password, IpAddress, Network, entities, ConnectionString);

                                if (localerror == null)
                                {
                                    result = new CashInTaskResult
                                    {
                                        TransactionHex = tx.ToHex(),
                                        TransactionHash = tx.GetHash().ToString()
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
