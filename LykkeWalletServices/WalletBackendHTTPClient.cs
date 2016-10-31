using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class WalletBackendHTTPClient : HttpClient
    {
        public async Task<HttpResponseMessage> GetAsync(string requestUri, int retryCount = 3)
        {
            // The benefit of retry over wait time is that in retries a new request is sent
            // while the first request maybe stuck
            // Manually tested with the help of Telerik Fiddler
            int retries = 0;
            while (true)
            {
                try
                {
                    return await base.GetAsync(requestUri);
                }
                catch (TaskCanceledException exp)
                {
                    retries++;

                    if (retries >= retryCount)
                    {
                        throw exp;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
    }
}
