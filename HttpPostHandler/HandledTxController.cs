using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace HttpPostHandler
{
    public class HandledTxController : ApiController
    {
        public class Error
        {
            public int Code
            {
                get;
                set;
            }

            public string Message
            {
                get;
                set;
            }
        }
        public class Response
        {
            public Error Error
            {
                get;
                set;
            }
        }

        public Response Get(int id)
        {
            var reply = new Response();
            reply.Error = new Error();
            reply.Error.Code = 10;
            reply.Error.Message = "Invalid input.";
            return reply;
        }

        public class LykkeJobsNotificationMessage
        {
            public string TransactionId
            {
                get;
                set;
            }

            public string BlockchainHash
            {
                get;
                set;
            }

            public string Operation
            {
                get;
                set;
            }
        }

        [HttpPost]
        public Response Post([FromBody]LykkeJobsNotificationMessage str)
        {
            Console.WriteLine(str);
            var reply = new Response();
            /*
            reply.Error = new Error();
            reply.Error.Code = 10;
            reply.Error.Message = "Invalid input.";
            */
            return reply;
            
        }
    }
}
