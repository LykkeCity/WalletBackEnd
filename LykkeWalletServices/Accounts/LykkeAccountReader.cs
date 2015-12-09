using System;
using System.Threading.Tasks;
using Core;

namespace LykkeWalletServices.Accounts
{
    public class LykkeAccountReader : ILykkeAccountReader
    {
        private readonly ILykkeCredentials _lykkeCredentials;

        public LykkeAccountReader(ILykkeCredentials lykkeCredentials)
        {
            _lykkeCredentials = lykkeCredentials;
        }

        public Task<AccountModel> GetAccountModel(string accountId)
        {
            /*
            if (accountId == _lykkeCredentials.PublicAddress)
            {
                var result = AccountModel.Create(_lykkeCredentials.PrivateKey, _lykkeCredentials.PublicAddress, _lykkeCredentials.CcPublicAddress, null);
                return Task.FromResult(result);
            }
            */
            // ToDo - implement getting Account Model
            // If we have our Lyyke accountId - then we should read from local database
            // If we have client accountId - then we should request service, which can resolve account model
            throw new NotImplementedException();
        }
    }
}
