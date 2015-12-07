namespace Core
{
    public class AccountModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string PrivateKey { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string PublicAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CcPublicAddress { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MultisigPublicAddress { get; set; }

        public static AccountModel Create(string privateKey, string publicAddress, string ccpublicKey,
            string multisigPublicAddress)
        {
            return new AccountModel
            {
                PublicAddress = publicAddress,
                CcPublicAddress = ccpublicKey,
                MultisigPublicAddress = multisigPublicAddress,
                PrivateKey = privateKey
            };
        }
    }

}
