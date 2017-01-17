using Common;
using Common.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices.TimerServices
{
    public class SrvTransactionSchedulerSender : TimerPeriod
    {
        public SrvTransactionSchedulerSender(ILog log, uint timerPeriodInMiliSeconds)
            : base("SrvTransactionSchedulerSender", (int)timerPeriodInMiliSeconds, log)
        {
        }

        protected override async Task Execute()
        {
            try
            {

            }
            catch(Exception exp)
            {

            }
        }
    }
}
