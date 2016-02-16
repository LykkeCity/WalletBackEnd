using NBitcoin;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvNetworkBase
    {
        protected Network Network
        {
            get; set;
        }
        protected OpenAssetsHelper.AssetDefinition[] Assets
        {
            get; set;
        }
        protected string Username
        {
            get; set;
        }
        protected string Password
        {
            get; set;
        }
        protected string IpAddress
        {
            get; set;
        }

        protected string FeeAddress
        {
            get; set;
        }

        protected string ConnectionString { get; set; }
        public SrvNetworkBase(Network network, OpenAssetsHelper.AssetDefinition[] assets,
            string username, string password, string ipAddress, string connectionString, string feeAddress)
        {
            this.Network = network;
            this.Assets = assets;
            this.Username = username;
            this.Password = password;
            this.IpAddress = ipAddress;
            this.ConnectionString = connectionString;
            this.FeeAddress = feeAddress;
        }
    }
}
