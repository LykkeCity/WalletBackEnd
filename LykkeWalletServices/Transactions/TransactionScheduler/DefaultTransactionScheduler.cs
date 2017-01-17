using Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TransactionScheduler
{
    public class DefaultTransactionScheduler : ITransactionScheduler
    {
        ConcurrentQueue<TransactionToDoBase> concurrentQueue
            = new ConcurrentQueue<TransactionToDoBase>();

        public void AddTransactionToScheduler(TransactionToDoBase tx)
        {
            concurrentQueue.Enqueue(tx);
        }

        public TransactionToDoBase[] GetListOfTransactionsToComplete
            (TransactionToDoBase[] completeTransactions)
        {
            IList<TransactionToDoBase> ret
                = new List<TransactionToDoBase>();

            TransactionToDoBase txToProcess = null;
            bool dequeueSuccessful = false;
            while (concurrentQueue.Count > 0)
            {
                dequeueSuccessful = concurrentQueue.TryDequeue(out txToProcess);
                if (dequeueSuccessful)
                {
                    ret.Add(txToProcess);
                }
            }

            return ret.ToArray();
        }
    }
}
