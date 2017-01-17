using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TransactionScheduler
{
    public interface ITransactionScheduler
    {
        void AddTransactionToScheduler(TransactionToDoBase tx);

        TransactionToDoBase[] GetListOfTransactionsToComplete(TransactionToDoBase[] completeTransactions);
    }
}
