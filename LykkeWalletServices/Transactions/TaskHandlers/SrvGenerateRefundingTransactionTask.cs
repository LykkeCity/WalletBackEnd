using Core;
using LykkeWalletServices.BlockchainManager;
using NBitcoin;
using NBitcoin.Policy;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: GenerateRefundingTransaction:{"TransactionId":"10","MultisigAddress":"2NDT6sp172w2Hxzkcp8CUQW9bB36EYo3NFU", "RefundAddress":"mt2rMXYZNUxkpHhyUhLDgMZ4Vfb1um1XvT", "timeoutInMinutes":360, "JustRefundTheNonRefunded":true, "FeeWillBeInsertedNow":false}
    // Sample response: GenerateRefundingTransaction:{"TransactionId":"10","Result":{"RefundTransaction":"xxx"},"Error":null}
    // If refund transaction is sent early, one gets "64: non-final (code -26)"
    public class SrvGenerateRefundingTransactionTask : SrvNetworkInvolvingExchangeBase
    {
        private static int generateRefundingTransactionMinimumConfirmationNumber = 0;

        public static int GenerateRefundingTransactionMinimumConfirmationNumber
        {
            get
            {
                return generateRefundingTransactionMinimumConfirmationNumber;
            }
            set
            {
                generateRefundingTransactionMinimumConfirmationNumber = value;
            }
        }

        public SrvGenerateRefundingTransactionTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string feePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
        }

        public async Task<Tuple<GenerateRefundingTransactionTaskResult, Error>> ExecuteTask(TaskToDoGenerateRefundingTransaction data)
        {
            // The coins to be refunded are two types, a- the ones which has never been refunded (newly arrived)
            /// b - The ones which their refunds has become invalid
            GenerateRefundingTransactionTaskResult result = null;
            Error error = null;
            Transaction refundTx = null;

            Func<int> getMinimumConfirmationNumber = (() => { return GenerateRefundingTransactionMinimumConfirmationNumber; });

            bool wholeRefund = !(data.JustRefundTheNonRefunded ?? false);

            try
            {
                for (int retryCount = 0; retryCount < OpenAssetsHelper.ConcurrencyRetryCount; retryCount++)
                {
                    try
                    {
                        BitcoinSecret exchangePrivateKey = null;

                        using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                        {
                            using (var transaction = entities.Database.BeginTransaction())
                            {
                                Script multiSigScript = null;
                                PubKey clientPubKey = null;
                                if (data.PubKey == null)
                                {
                                    var multiSig = (await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities));
                                    multiSigScript = new Script(multiSig.MultiSigScript);
                                    clientPubKey = (new BitcoinSecret(multiSig.WalletPrivateKey)).PubKey;
                                    exchangePrivateKey = clientPubKey.GetExchangePrivateKey(entities);
                                }
                                else
                                {
                                    clientPubKey = new PubKey(data.PubKey);
                                    exchangePrivateKey = clientPubKey.GetExchangePrivateKey(entities);
                                    multiSigScript = PayToMultiSigTemplate.Instance.GenerateScriptPubKey
                                        (2, new PubKey[] { clientPubKey, exchangePrivateKey.PubKey });

                                }
                                var multiSigAddress = multiSigScript.GetScriptAddress(connectionParams.BitcoinNetwork).ToString();

                                DateTimeOffset lockTimeValue = new DateTimeOffset(DateTime.UtcNow) + new TimeSpan(0, (int)data.timeoutInMinutes, 0);
                                LockTime lockTime = new LockTime(lockTimeValue);

                                var walletOuputs = await LykkeBitcoinBlockchainManager.GetWalletOutputs(multiSigAddress,
                                    connectionParams.BitcoinNetwork, entities, getMinimumConfirmationNumber);
                                if (walletOuputs.Item2)
                                {
                                    error = new Error();
                                    error.Code = ErrorCode.ProblemInRetrivingWalletOutput;
                                    error.Message = walletOuputs.Item3;
                                }
                                else
                                {
                                    if (walletOuputs.Item4)
                                    {
                                        error = new Error();
                                        error.Code = ErrorCode.RaceWithRefund;
                                        error.Message = "At least one of wallet outputs has been used in a now activated refund.";
                                        break;
                                    }
                                    else
                                    {
                                        var toBeRefundedCoins = new List<Coin>();
                                        var toBeRefundedColoredCoins = new List<ColoredCoin>();

                                        // var coins = OpenAssetsHelper.GetColoredUnColoredCoins(walletOuputs.Item1, null);
                                        var coins = OpenAssetsHelper.GetColoredUnColoredCoins(walletOuputs.Item1, "All", Assets);

                                        if (!wholeRefund)
                                        {
                                            throw new NotSupportedException("This type of refund request is more supported.");

                                            // ToDo: Possible bug fix for "System.Data.Entity.Core.EntityCommandExecutionException: An error occurred while executing the command definition. See the inner exception for details. --->System.InvalidOperationException: There is already an open DataReader associated with this Command which must be closed first.
                                            // ToList may cause performance issues, but it should be neligable
                                            var previousUnspentCoins = (from c in entities.RefundedOutputs
                                                                        where c.RefundedAddress.Equals(multiSigAddress) && c.HasBeenSpent.Equals(false)
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
                                                        (cr.RefundedTxId, connectionParams);
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
                                        }
                                        else
                                        {
                                            // The whole refund method
                                            toBeRefundedCoins.AddRange(coins.Item2);

                                            // Colored coins added
                                            toBeRefundedColoredCoins.AddRange(coins.Item1);
                                        }

                                        if (toBeRefundedCoins.Count == 0 && toBeRefundedColoredCoins.Count == 0)
                                        {
                                            error = new Error();
                                            error.Code = ErrorCode.NoCoinsToRefund;
                                            error.Message = "There are no coins to be refunded.";
                                        }

                                        if (error == null)
                                        {
                                            IDestination dest = null;
                                            if (string.IsNullOrEmpty(data.RefundAddress))
                                            {
                                                dest = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(clientPubKey.GetAddress(connectionParams.BitcoinNetwork).ToWif());
                                            }
                                            else
                                            {
                                                dest = OpenAssetsHelper.GetBitcoinAddressFormBase58Date(data.RefundAddress);
                                            }

                                            IList<ScriptCoin> scriptCoinsToBeRefunded = new List<ScriptCoin>();
                                            IList<ColoredCoin> coloredScriptCoinsToBeRefunded = new List<ColoredCoin>();
                                            foreach (var item in toBeRefundedCoins)
                                            {
                                                scriptCoinsToBeRefunded.Add(new ScriptCoin(item, multiSigScript));
                                            }

                                            long totalFeesRequiredForColoredCoins = 0;
                                            var coloredOutputFee = (new TxOut(Money.Zero, dest.ScriptPubKey).GetDustThreshold(new FeeRate(Money.Satoshis(5000)))).Satoshi;
                                            foreach (var item in toBeRefundedColoredCoins)
                                            {
                                                coloredScriptCoinsToBeRefunded.Add(new ColoredCoin(item.Amount, new ScriptCoin(item, multiSigScript)));
                                                totalFeesRequiredForColoredCoins += Math.Max(coloredOutputFee - item.Bearer.Amount, 0);
                                            }

                                            TransactionBuilder builder = new TransactionBuilder();
                                            Transaction tx = new Transaction();

                                            var feeBeInsertedNow = data.FeeWillBeInsertedNow ?? true;
                                            long feeAmount = 0;
                                            long destAmount = 0;

                                            if (feeBeInsertedNow)
                                            {
                                                feeAmount = OpenAssetsHelper.TransactionSendFeesInSatoshi;
                                                destAmount = scriptCoinsToBeRefunded.Sum(c => c.Amount) - feeAmount - totalFeesRequiredForColoredCoins;
                                                if (destAmount < 0)
                                                {
                                                    throw new Exception
                                                        ("The amount to be refunded is smaller than the fee, no refund will be generated.");
                                                }
                                            }
                                            else
                                            {
                                                feeAmount = (long)OpenAssetsHelper.BTCToSathoshiMultiplicationFactor - totalFeesRequiredForColoredCoins;
                                            }

                                            builder = builder
                                            .SetLockTime(lockTime)
                                            .AddKeys(exchangePrivateKey)
                                            .AddCoins(scriptCoinsToBeRefunded)
                                            .AddCoins(coloredScriptCoinsToBeRefunded);
                                            if (!feeBeInsertedNow)
                                            {
                                                Script fakeScript = null;
                                                fakeScript = multiSigScript;
                                                Coin fakeCoin = new Coin(new uint256(0), 0, new Money(100000000), fakeScript);
                                                builder = builder.AddCoins(fakeCoin);
                                            }

                                            foreach (var item in toBeRefundedColoredCoins)
                                            {
                                                builder = builder.SendAsset(dest, item.Amount);
                                            }

                                            SigHash sigHash = SigHash.All;
                                            // The default value is set to true to be compatible with the orevious implementation
                                            if (feeBeInsertedNow)
                                            {
                                                refundTx = builder.Send(dest, destAmount)
                                                // refundTx = builder.Send(dest, 1)
                                                .SendFees(new Money(feeAmount))
                                                .SetChange(dest).BuildTransaction(false);
                                            }
                                            else
                                            {
                                                foreach (var item in toBeRefundedCoins)
                                                {
                                                    builder = builder.Send(dest, item.Amount);
                                                }

                                                sigHash = SigHash.All | SigHash.AnyoneCanPay;
                                                refundTx = builder.SendFees(new Money(feeAmount)).SetChange(dest).BuildTransaction(false, sigHash);
                                            }

                                            refundTx.Inputs[0].Sequence = Sequence.SEQUENCE_FINAL - 1;

                                            refundTx = builder.SignTransactionInPlace(refundTx, sigHash);

                                            if (!feeBeInsertedNow)
                                            {
                                                TxIn toBeRemovedInput = null;
                                                foreach (var item in refundTx.Inputs)
                                                {
                                                    if (item.PrevOut.Hash == new uint256(0))
                                                    {
                                                        toBeRemovedInput = item;
                                                    }
                                                }
                                                refundTx.Inputs.Remove(toBeRemovedInput);
                                            }


                                            if (!wholeRefund)
                                            {
                                                throw new NotSupportedException("This type of refund request is more supported.");
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
                                                    refunded.RefundedAddress = multiSigAddress;
                                                    refunded.RefundedOutputNumber = (int)item.Outpoint.N;
                                                    refunded.RefundTransaction = refund;
                                                    refunded.RefundedTxId = item.Outpoint.Hash.ToString();
                                                    entities.RefundedOutputs.Add(refunded);
                                                }
                                                entities.SaveChanges();
                                            }
                                            else
                                            {
                                                var refund = new WholeRefund();
                                                refund.BitcoinAddress = multiSigAddress;
                                                refund.CreationTime = DateTime.UtcNow;
                                                refund.LockTime = lockTime.Date.DateTime;
                                                refund.TransactionHex = refundTx.ToHex();
                                                refund.TransactionId = refundTx.GetHash().ToString();
                                                entities.WholeRefunds.Add(refund);

                                                WholeRefundSpentOutput spentOutput = null;
                                                foreach (var input in refundTx.Inputs)
                                                {
                                                    spentOutput = new WholeRefundSpentOutput();
                                                    spentOutput.WholeRefund = refund;
                                                    spentOutput.SpentTransactionId = input.PrevOut.Hash.ToString();
                                                    spentOutput.SpentTransactionOutputNumber = (int)input.PrevOut.N;
                                                    entities.WholeRefundSpentOutputs.Add(spentOutput);
                                                }

                                                entities.SaveChanges();
                                            }
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
