using Core;
using NBitcoin;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvNetworkInvolvingExchangeBase : SrvNetworkBase
    {
        public SrvNetworkInvolvingExchangeBase(Network network, AssetDefinition[] assets,
            string username, string password, string ipAddress, string feeAddress, string connectionString)
            : base(network, assets, username, password, ipAddress, connectionString, feeAddress)
        {
            FeeAddress = feeAddress;
        }
    }
}
