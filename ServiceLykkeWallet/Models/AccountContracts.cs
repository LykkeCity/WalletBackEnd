using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceLykkeWallet.Models
{
    public class GenerateAccountContract
    {
        /// <summary>
        /// The network whih the address resides in, valid values are Main and TestNet
        /// </summary>
        public string Network
        {
            get;
            set;
        }

        /// <summary>
        /// The private key for the wallet, in wif format
        /// </summary>
        /// <remarks>The properties values are in the base58 format</remarks>
        public string PrivateKey
        {
            get;
            set;
        }

        // ToDo - clarify things
        /// <summary>
        /// The feature proposed for KYC, under investigation and clarification
        /// </summary>
        // public string MultisigPublicAddress { get; set; }

        /// <summary>
        /// The public address for the wallet in base58, this could also be generatated from the private key
        /// </summary>
        /// <remarks>This property could not be set since it is derived from private key.</remarks>
        public string WalletAddress
        {
            get;
            set;
        }

        

        public GenerateAccountContract()
        {
        }
    }

    
}
