using NBitcoin;
using System;

namespace Core
{
    public class AccountModel
    {
        /// <summary>
        /// The private variable holding the value for the private key for the account
        /// </summary>
        public Key Key
        {
            get;
            set;
        }

        public NetworkType NetworkType
        {
            get;
            set;
        }

        /*
        /// <summary>
        /// The network for which the address resides in
        /// </summary>
        private NetworkType network = NetworkType.Main;

        /// <summary>
        /// The network whih the address resides in, valid values are Main and TestNet
        /// </summary>
        public string Network
        {
            get
            {
                return network.ToString();
            }
            set
            {
                if (!Enum.TryParse<NetworkType>(value, out network))
                {
                    throw new ArgumentOutOfRangeException("Network");
                }
            }

        }
        
        

        /// <summary>
        /// The base56 
        /// </summary>
        public string PrivateKey
        {
            get
            {
                return key.ToString(GetNetwork());
            }

            set
            {
                key = new BitcoinSecret(value).PrivateKey;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string WalletAddress
        {
            get
            {
                return key.PubKey.GetAddress(GetNetwork()).ToWif();
            }
        }
        */

        /// <summary>
        /// ToDo - To be decided upon later.
        /// </summary>
        /// public string MultisigPublicAddress { get; set; }

        public static AccountModel Create(Key key, NetworkType type)
        {
            return new AccountModel
            {
                Key = key,
                NetworkType = type
            };
        }
    }

    // ToDo - Find if there is a better way using NBitcoin, not to define it here
    /// <summary>
    /// The network type enum
    /// </summary>
    public enum NetworkType
    {
        Main,
        TestNet
    }
}
