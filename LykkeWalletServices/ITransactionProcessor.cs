using Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public interface ITransactionProcessor
    {
        Task Process(TransactionToDoBase @event);
    }
}
