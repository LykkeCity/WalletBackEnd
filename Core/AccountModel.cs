namespace Core
{
    public class AccountModel
    {
        public string PrivateKey { get; set; }
        public string PublicAddress { get; set; }
        public string CcPublicAddress { get; set; }
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
