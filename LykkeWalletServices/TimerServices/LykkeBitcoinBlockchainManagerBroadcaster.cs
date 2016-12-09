using Common;
using Common.Log;
using LykkeWalletServices.BlockchainManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.TimerServices
{
    public class LykkeBitcoinBlockchainManagerBroadcaster : TimerPeriod
    {
        public LykkeBitcoinBlockchainManagerBroadcaster(ILog log) :
            base("LykkeBitcoinBlockchainManagerBroadcaster", 2*60*1000, log )
        {
        }

        protected override async Task Execute()
        {
            await LykkeBitcoinBlockchainManager.BroadcastAsManyTransactionsAsPossible();
        }
    }
}
