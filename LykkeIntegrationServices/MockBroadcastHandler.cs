using Core.LykkeIntegration.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LykkeIntegrationServices
{
    public class MockBroadcastHandler : IPreBroadcastHandler
    {
        public async Task<HandleTxError> HandleTx(HandleTxRequest request)
        {
            try
            {
                System.Console.WriteLine("*******************************");
                System.Console.WriteLine(request.ToString());
                System.Console.WriteLine("*******************************");
                return null;
            }
            catch(Exception e)
            {
                return new HandleTxError { ErrorMessage = e.ToString(), ErrorCode = 0 };
            }
        }
    }
}
