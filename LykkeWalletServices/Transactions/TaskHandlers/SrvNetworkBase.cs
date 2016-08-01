using Core;
using NBitcoin;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvNetworkBase
    {
        protected AssetDefinition[] Assets
        {
            get; set;
        }
        
        protected string FeeAddress
        {
            get; set;
        }

        protected string ConnectionString { get; set; }

        protected OpenAssetsHelper.RPCConnectionParams connectionParams = null;
        public SrvNetworkBase(Network network, AssetDefinition[] assets,
            string username, string password, string ipAddress, string connectionString, string feeAddress)
        {
            this.Assets = assets;
            this.connectionParams = new OpenAssetsHelper.RPCConnectionParams
            { Username = username, Password = password, IpAddress = ipAddress, Network = network.ToString() };
            this.ConnectionString = connectionString;
            this.FeeAddress = feeAddress;
        }
    }
}
