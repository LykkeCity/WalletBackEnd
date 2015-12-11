using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.Responses
{
    public class TaskResultBase
    {
        public bool HasErrorOccurred { get; set; }
        public string ErrorMessage { get; set; }
    }
}
