using LykkeWalletServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Log
{
    public class LogToDB : ILog
    {
        public Task WriteInfo(string component, string process, string context, string info, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            throw new NotImplementedException();
        }
        public Task WriteWarning(string component, string process, string context, string info, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            throw new NotImplementedException();
        }
        public Task WriteError(string component, string process, string context, Exception exeption, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            try
            {
                var entities = (SqlexpressLykkeEntities)extraLoggerParam;
                entities.DBLogs.Add(new DBLog
                {
                    CreationDate = DateTime.Now,
                    Message = string.Format("The error is: Component: {0} , Process: {1} , Context: {2} , exception {3} ",
                    component, process, context, exeption.ToString())
                });
                entities.SaveChanges();
            }
            catch(Exception)
            {
            }
            return Task.FromResult(0);
        }

        public Task WriteFatalError(string component, string process, string context, Exception exeption, DateTime? dateTime = null, object extraLoggerParam = null)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
