using Common;
using Common.Log;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LykkeWalletServices.OpenAssetsHelper;

namespace LykkeWalletServices
{
    public class SrvUnsignedTransactionsUpdater : TimerPeriod
    {
        private int unsignedTransactionTimeoutInMinutes = 0;
        private string connectionString = null;

        public SrvUnsignedTransactionsUpdater(ILog log, int unsignedTransactionTimeoutInMinutes, int unsignedTransactionsUpdaterPeriod, string connectionString) :
            base("SrvUnsignedTransactionsUpdater", unsignedTransactionsUpdaterPeriod , log)
        {
            this.unsignedTransactionTimeoutInMinutes = unsignedTransactionTimeoutInMinutes;
            this.connectionString = connectionString;
        }

        protected override async Task Execute()
        {
            for (int concurrencyCount = 0; concurrencyCount < 3; concurrencyCount++)
            {
                try
                {
                    var unsignedTransactionsPastTime = DateTime.UtcNow.AddMinutes(-1 * unsignedTransactionTimeoutInMinutes);
                    using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
                    {
                        using (var dbTransaction = entities.Database.BeginTransaction())
                        {
                            var timedouts = (from r in entities.UnsignedTransactions
                                             where (r.CreationTime < unsignedTransactionsPastTime || r.CreationTime == null) &&
                                             (r.HasTimedout ?? false) == false &&
                                             (r.TransactionSendingSuccessful ?? false) == false &&
                                             r.TransactionIdWhichMadeThisTransactionInvalid == null
                                             select r).ToList();

                            for (int i = 0; i < timedouts.Count(); i++)
                            {
                                var record = timedouts[i];
                                record.HasTimedout = true;
                            }
                            await entities.SaveChangesAsync();

                            for (int i = 0; i < timedouts.Count(); i++)
                            {
                                var record = timedouts[i];

                                var freedOutput = (from output in entities.UnsignedTransactionSpentOutputs
                                                   where output.UnsignedTransactionId == record.id
                                                   select output).ToList();

                                foreach (var item in freedOutput)
                                {
                                    var transactionsWithFreedOutput = (from output in entities.UnsignedTransactionSpentOutputs
                                                                       join tr in entities.UnsignedTransactions on output.UnsignedTransactionId equals tr.id
                                                                       where output.TransactionId == item.TransactionId &&
                                                                       output.OutputNumber == item.OutputNumber &&
                                                                       (tr.HasTimedout ?? false) == false &&
                                                                       tr.TransactionIdWhichMadeThisTransactionInvalid == null
                                                                       select output.UnsignedTransactionId).Count();

                                    if (transactionsWithFreedOutput > 0)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        await CheckForFeeOutputAndFree(entities, item);
                                        await entities.SaveChangesAsync();
                                    }
                                }
                            }

                            dbTransaction.Commit();
                        }
                    }
                }
                catch (OptimisticConcurrencyException ex)
                {
                    await _log.WriteError("UnsignedTransacionUpdater", "", "", ex);
                    continue;
                }
                catch (Exception ex)
                {
                    await _log.WriteError("UnsignedTransacionUpdater", "", "", ex);
                    break;
                }

                break;
            }
        }
    }
}
