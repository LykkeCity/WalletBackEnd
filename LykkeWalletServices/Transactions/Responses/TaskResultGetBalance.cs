using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.Responses
{
    public class TaskResultGetBalance : TaskResultBase
    {
        public float Balance { get; set; }
        public float UnconfirmedBalance { get; set; }
    }
}
