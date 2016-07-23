using NBitcoin;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace LykkeWalletServices
{
    public class LykkeExtenddedRPCClient : RPCClient
    {
        public LykkeExtenddedRPCClient(NetworkCredential credentials,
            string host, Network network) : base(credentials, host, network)
        {
        }

        public LykkeExtenddedRPCClient(NetworkCredential credentials,
            Uri address, Network network = null) : base(credentials, address, network)
        {
        }
        
        public async Task<IEnumerable<string>> GenerateBlocksAsync(int count)
        {
            RPCResponse response = await SendCommandAsync("generate", new object[] { count });
            if (response.Error != null)
            {
                throw new RPCException(response.Error.Code, response.Error.Message, response);
            }
            else
            {
                return response.Result.Select(c => (string)c);
            }
        }

        public async Task<string> GetTxOut(string txid, uint n)
        {
            RPCResponse response = await SendCommandAsync("gettxout", new object[] { txid, n });
            if (response.Error != null)
            {
                throw new RPCException(response.Error.Code, response.Error.Message, response);
            }
            else
            {
                return response.Result.Select(c => (string)c).FirstOrDefault();
            }
        }
    }
}
