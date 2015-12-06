using System.Collections.Generic;

namespace LykkeWalletServices.Accounts
{
    public class AccountBalance
    {
        public string Asset { get; set; }
        public double Amount { get; set; }
    }

    public class SrvAccountBalanceAccess
    {

        /// <summary>
        /// Get Account balance for all assets
        /// </summary>
        /// <param name="publicAdress">Bitcoin public address</param>
        /// <returns>List of balances of all assets</returns>
        public IEnumerable<AccountBalance> GetAccountBalances(string publicAdress)
        {
            // ToDo - Implement Account Balance
            return new AccountBalance[0];
        } 
    }
}
