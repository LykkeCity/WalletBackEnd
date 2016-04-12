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
    // Sample request: GenerateRefundingTransaction:{"TransactionId":"10","MultisigAddress":"2MuKSMmP4iqdFj9uNpGxKs2wujmTvDotchG","TransactionHash":"b73d3ecaa546bd03fd5742b927211047e9ab508c4bfc407e8fd31f1d90cef244", "timeoutInMinutes":360}
    // Sample response: GenerateRefundingTransaction:{"TransactionId":"10","Result":{"RefundTransaction":"xxx"},"Error":null}
    // If refund transaction is sent early, one gets "64: non-final (code -26)"
    public class SrvGenerateRefundingTransactionTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvGenerateRefundingTransactionTask(Network network, OpenAssetsHelper.AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string feePrivateKey, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<GenerateRefundingTransactionTaskResult, Error>> ExecuteTask(TaskToDoGenerateRefundingTransaction data)
        {
            GenerateRefundingTransactionTaskResult result = null;
            Error error = null;
            try
            {
                for (int retryCount = 0; retryCount < OpenAssetsHelper.ConcurrencyRetryCount; retryCount++)
                {
                    try
                    {
                        PubKey exchangePubKey = (new BitcoinSecret(ExchangePrivateKey, Network)).PubKey;

                        using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                var multiSig = (await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities));
                                PubKey clientPubKey = (new BitcoinSecret(multiSig.WalletPrivateKey)).PubKey;
                                LockTime lockTime = new LockTime(new DateTimeOffset(DateTime.UtcNow) + new TimeSpan(0, (int)data.timeoutInMinutes, 0));

                                var transactionResult = await OpenAssetsHelper.GetTransactionHex(data.TransactionHash, Network, Username, Password, IpAddress);
                                if (transactionResult.Item1)
                                {
                                    error = new Error
                                    {
                                        Code = ErrorCode.ProblemInRetrivingTransaction,
                                        Message = transactionResult.Item2
                                    };
                                }
                                else
                                {
                                    Coin sourceCoin = null;
                                    Transaction tx = new Transaction(transactionResult.Item3);
                                    foreach (var item in tx.Outputs)
                                    {
                                        if (OpenAssetsHelper.GetAddressFromScriptPubKey(item.ScriptPubKey, Network)
                                            == multiSig.MultiSigAddress)
                                        {
                                            sourceCoin = new Coin(tx, item);
                                            break;
                                        }
                                    }

                                    ScriptCoin sourceScriptCoin = new ScriptCoin(sourceCoin, new Script(multiSig.MultiSigScript));
                                    TransactionBuilder builder = new TransactionBuilder();
                                    var refundTx = builder
                                        .SetLockTime(lockTime)
                                        .AddKeys(new BitcoinSecret(multiSig.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                                        .AddCoins(sourceScriptCoin)
                                        .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi)) // We have 2 colored coin outputs
                                        .SetChange(BitcoinAddress.Create(multiSig.WalletAddress, Network))                                       .BuildTransaction(false);

                                    refundTx.Inputs[0].Sequence = Sequence.SEQUENCE_FINAL - 1;

                                    refundTx = builder.SignTransactionInPlace(refundTx);

                                    bool verify = builder.Verify(refundTx);

                                    result = new GenerateRefundingTransactionTaskResult
                                    {
                                        RefundTransaction = refundTx.ToHex()
                                    };

                                    if (error == null)
                                    {
                                        transaction.Commit();
                                        break;
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                    }
                                }
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
            return new Tuple<GenerateRefundingTransactionTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGenerateRefundingTransaction data, Func<Tuple<GenerateRefundingTransactionTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
