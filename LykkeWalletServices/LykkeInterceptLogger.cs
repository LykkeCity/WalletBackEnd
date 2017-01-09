using Common.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class LykkeInterceptLogger : ILog
    {
        public async Task Log(string info, DateTime? dateTime = null)
        {
            using (SqlexpressLykkeEntities entities
                = new SqlexpressLykkeEntities(WebSettings.ConnectionString))
            {
                entities.DBLogs.Add(new DBLog { Message = info, CreationDate = dateTime ?? DateTime.UtcNow });
                await entities.SaveChangesAsync();
            }
        }

        public async Task WriteInfo(string component, string process, string context,
            string info, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            await Log(info, dateTime);
        }
        public async Task WriteWarning(string component, string process, string context,
            string info, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            await Log(info, dateTime);
        }

        public async Task WriteError(string component, string process, string context,
            Exception exeption, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            await Log(exeption.ToString(), dateTime);
        }

        public async Task WriteFatalError(string component, string process, string context,
            Exception exeption, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            await Log(exeption.ToString(), dateTime);
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }
    }
}
