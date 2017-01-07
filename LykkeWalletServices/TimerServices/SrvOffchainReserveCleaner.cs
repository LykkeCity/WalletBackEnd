using Common;
using Common.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class SrvOffchainReserveCleaner : TimerPeriod
    {
        private string connectionString = null;

        public SrvOffchainReserveCleaner(ILog log, string connectionString)
            : base ("SrvOffchainReserveCleaner", 2* 60 * 1000, log)
        {
            this.connectionString = connectionString;
        }

        protected override async Task Execute()
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
            {
                var reserveds = from r in entities.ChannelCoins
                                where r.ReservationEndDate < DateTime.UtcNow && (r.ReservationTimedout ?? false) == false && (r.ReservationFinalized ?? false) == false
                                select r;

                bool changed = false;
                foreach(var item in reserveds)
                {
                    item.ReservationTimedout = true;
                    changed = true;
                }

                if (changed)
                {
                    await entities.SaveChangesAsync();
                }
            }
        }
    }
}
