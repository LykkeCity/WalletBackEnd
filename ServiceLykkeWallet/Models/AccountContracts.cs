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
        /// 
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string MultisigPublicAddress { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public string CcPublicAddress { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PublicAddress { get; set; }
    }
}
