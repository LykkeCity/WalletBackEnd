using Common;
using Common.Log;
using LykkeWalletServices.TimerServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class SrvFeeUpdater : TimerPeriod
    {
        public SrvFeeUpdater(ILog log) : base ("SrvFeeUpdater", 60 * 60 * 1000, log)
        {
        }

        protected override async Task Execute()
        {
            OpenAssetsHelper.TransactionSendFeesInSatoshi = await OpenAssetsHelper.UpdateFeeRateFromInternet();
        }
    }
}
