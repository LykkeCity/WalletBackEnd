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

        public SrvFeeReserveCleaner(ILog log, string connectionString) : base ("SrvFeeReserveCleaner", 10* 60 * 1000, log)
        {
            this.connectionString = connectionString;
        }

        protected override async Task Execute()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
            {
                var reserveds = from r in entities.PregeneratedReserves
                                where r.ReservationEndDate == null ? r.CreationTime < fiveMinutesAgo : r.ReservationEndDate < DateTime.UtcNow
                                select r; 

                entities.PregeneratedReserves.RemoveRange(reserveds);

                await entities.SaveChangesAsync();
            }
        }
    }
}
