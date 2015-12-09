using System;
using Core;

namespace LykkeWalletServices.Accounts
{


    public class SrvAccountGenerator
    {
        public AccountModel GenerateAccount(NetworkType network)
        {
            return new AccountModel
            {
                Key = new NBitcoin.Key(),
                NetworkType = network
            };
        }

    }
}
