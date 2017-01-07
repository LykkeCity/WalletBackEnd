using Common;
using Common.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class SrvFeeReserveCleaner : TimerPeriod
    {
        private string connectionString = null;

        public SrvFeeReserveCleaner(ILog log, string connectionString) : base("SrvFeeReserveCleaner", 10 * 60 * 1000, log)
        {
            this.connectionString = connectionString;
        }

        protected override async Task Execute()
        {
            try
            {
                var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
                {
                    var reserveds = from r in entities.PregeneratedReserves
                                    where r.ReservationEndDate == null ? r.CreationTime < fiveMinutesAgo : r.ReservationEndDate < DateTime.UtcNow
                                    select r;

                    foreach (var item in reserveds)
                    {
                        if ((item.ReservedForAddress ?? string.Empty).ToLower().StartsWith("nonautomatic"))
                        {
                            if (await OpenAssetsHelper.PregeneratedHasBeenSpentInBlockchain
                                (new PreGeneratedOutput { TransactionId = item.PreGeneratedOutputTxId, OutputNumber = item.PreGeneratedOutputN }, WebSettings.ConnectionParams))
                            {
                                item.PreGeneratedOutput.Consumed = 1;
                            }
                            item.PreGeneratedOutput.ReservedForAddress = null;
                        }
                    }

                    entities.PregeneratedReserves.RemoveRange(reserveds);

                    await entities.SaveChangesAsync();
                }

            }
            catch(Exception exp)
            {
            }
        }
    }
}
