using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.Responses
{
    public class UnsignedClientCommitmentTransactionResponse
    {
        public string FullySignedSetupTransaction
        {
            get;
            set;
        }

        public string UnsignedClientCommitment0
        {
            get;
            set;
        }
    }
}
