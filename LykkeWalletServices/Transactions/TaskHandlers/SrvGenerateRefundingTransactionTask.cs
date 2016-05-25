using Core;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: GenerateRefundingTransaction:{"TransactionId":"10","MultisigAddress":"2Mvny9kSaUJhbCTMWUaFQPYERCSrduKJjD4", "timeoutInMinutes":360}
    // Sample response: GenerateRefundingTransaction:{"TransactionId":"10","Result":{"RefundTransaction":"xxx"},"Error":null}
    // If refund transaction is sent early, one gets "64: non-final (code -26)"
    public class SrvGenerateRefundingTransactionTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvGenerateRefundingTransactionTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string feePrivateKey, string exchangePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, exchangePrivateKey, connectionString)
        {
        }

        public async Task<Tuple<GenerateRefundingTransactionTaskResult, Error>> ExecuteTask(TaskToDoGenerateRefundingTransaction data)
        {
            // The coins to be refunded are two types, a- the ones which has never been refunded (newly arrived)
            /// b - The ones which their refunds has become invalid
            GenerateRefundingTransactionTaskResult result = null;
            Error error = null;
            Transaction refundTx = null;
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
                                DateTimeOffset lockTimeValue = new DateTimeOffset(DateTime.UtcNow) + new TimeSpan(0, (int)data.timeoutInMinutes, 0);
                                LockTime lockTime = new LockTime(lockTimeValue);

                                var walletOuputs = await OpenAssetsHelper.GetWalletOutputs(multiSig.MultiSigAddress, Network, entities, false);
                                if (walletOuputs.Item2)
                                {
                                    error = new Error();
                                    error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                                    error.Message = walletOuputs.Item3;
                                }
                                else
                                {
                                    var toBeRefundedCoins = new List<Coin>();

                                    var coins = await OpenAssetsHelper.GetColoredUnColoredCoins(walletOuputs.Item1, null,
                                        Network, Username, Password, IpAddress);

                                    // ToDo: Possible bug fix for "System.Data.Entity.Core.EntityCommandExecutionException: An error occurred while executing the command definition. See the inner exception for details. --->System.InvalidOperationException: There is already an open DataReader associated with this Command which must be closed first.
                                    // ToList may cause performance issues, but it should be neligable
                                    var previousUnspentCoins = (from c in entities.RefundedOutputs
                                                                where c.RefundedAddress.Equals(multiSig.MultiSigAddress) && c.HasBeenSpent.Equals(false)
                                                                select c).ToList();

                                    // Finding the coins which has never been refunded
                                    foreach (var item in coins.Item2)
                                    {
                                        string transactionHash = item.Outpoint.Hash.ToString();
                                        int outputNumber = (int)item.Outpoint.N;
                                        var foundInDB = previousUnspentCoins.Where(uc => uc.RefundedTxId.Equals(transactionHash) && uc.RefundedOutputNumber.Equals(outputNumber)).Any();
                                        if (!foundInDB)
                                        {
                                            toBeRefundedCoins.Add(item);
                                        }
                                    }

                                    // Finding the coins which refund transaction has become invalid
                                    IList<string> refunfTxIdRepeated = new List<string>();
                                    foreach (var item in previousUnspentCoins)
                                    {
                                        var stillUnspent = (from c in coins.Item2
                                                            where c.Outpoint.Hash.ToString().Equals(item.RefundedTxId) && c.Outpoint.N.Equals((uint)item.RefundedOutputNumber)
                                                            select c).Any();

                                        if (!stillUnspent)
                                        {
                                            item.HasBeenSpent = true;
                                            refunfTxIdRepeated.Add(item.RefundTransaction.RefundTxId);
                                        }
                                    }
                                    entities.SaveChanges();

                                    // Marking the coins with the specific refunds as having invalid refunds
                                    var refunfTxIdNonRepeated = refunfTxIdRepeated.Distinct();
                                    Transaction tempTr = new Transaction();
                                    foreach (var item in refunfTxIdNonRepeated)
                                    {
                                        var refundCorrespondents = from c in entities.RefundedOutputs
                                                                   where c.HasBeenSpent == false && c.RefundTransaction.RefundTxId.Equals(item)
                                                                   select c;

                                        foreach (var cr in refundCorrespondents)
                                        {
                                            cr.RefundInvalid = true;
                                            var getTransactionRetValue = await OpenAssetsHelper.GetTransactionHex
                                                (cr.RefundedTxId, Network, Username, Password, IpAddress);
                                            if (getTransactionRetValue.Item1)
                                            {
                                                error = new Error();
                                                error.Code = ErrorCode.ProblemInRetrivingTransaction;
                                                error.Message = getTransactionRetValue.Item2;
                                                break;
                                            }

                                            toBeRefundedCoins.Add(new Coin(new Transaction(getTransactionRetValue.Item3),
                                                (uint)cr.RefundedOutputNumber));
                                        }
                                    }
                                    entities.SaveChanges();

                                    if (toBeRefundedCoins.Count == 0)
                                    {
                                        error = new Error();
                                        error.Code = ErrorCode.NoCoinsToRefund;
                                        error.Message = "There are no coins to be refunded.";
                                    }

                                    if (error == null)
                                    {
                                        IList<ScriptCoin> scriptCoinsToBeRefunded = new List<ScriptCoin>();
                                        foreach (var item in toBeRefundedCoins)
                                        {
                                            scriptCoinsToBeRefunded.Add(new ScriptCoin(item, new Script(multiSig.MultiSigScript)));
                                        }

                                        TransactionBuilder builder = new TransactionBuilder();
                                        refundTx = builder
                                            .SetLockTime(lockTime)
                                            .AddKeys(new BitcoinSecret(multiSig.WalletPrivateKey), new BitcoinSecret(ExchangePrivateKey))
                                            .AddCoins(scriptCoinsToBeRefunded)
                                            .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi)) 
                                            .SetChange(BitcoinAddress.Create(multiSig.WalletAddress, Network)).BuildTransaction(false);

                                        refundTx.Inputs[0].Sequence = Sequence.SEQUENCE_FINAL - 1;

                                        refundTx = builder.SignTransactionInPlace(refundTx);

                                        bool verify = builder.Verify(refundTx);

                                        // Adding the refunded outputs to DB
                                        var refund = new RefundTransaction();
                                        refund.RefundTxId = refundTx.GetHash().ToString();
                                        refund.RefundTxHex = refundTx.ToHex();
                                        entities.RefundTransactions.Add(refund);
                                        entities.SaveChanges();

                                        foreach (var item in toBeRefundedCoins)
                                        {
                                            var refunded = new RefundedOutput();
                                            refunded.LockTime = lockTimeValue.UtcDateTime;
                                            refunded.RefundedAddress = multiSig.MultiSigAddress;
                                            refunded.RefundedOutputNumber = (int)item.Outpoint.N;
                                            refunded.RefundTransaction = refund;
                                            refunded.RefundedTxId = item.Outpoint.Hash.ToString();
                                            entities.RefundedOutputs.Add(refunded);
                                        }
                                        entities.SaveChanges();

                                        result = new GenerateRefundingTransactionTaskResult
                                        {
                                            RefundTransaction = refundTx.ToHex()
                                        };
                                    }

                                    if (error == null)
                                    {
                                        transaction.Commit();
                                        break;
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        if (error.Code == ErrorCode.NoCoinsToRefund)
                                        {
                                            break;
                                        }
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
