using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvDepositWithdrawTaskHandler
    {

        public class TransactionToken
        {

        }


        private readonly ILykkeAccountReader _lykkeAccountReader;

        public SrvDepositWithdrawTaskHandler(ILykkeAccountReader lykkeAccountReader)
        {
            _lykkeAccountReader = lykkeAccountReader;
        }


        /// <summary>
        /// Do exchange procedure. If it has async stuff - make sure that procedure returns Task
        /// </summary>
        /// <param name="accFrom">account from</param>
        /// <param name="accTo">account to</param>
        /// <param name="asset">Asset Id</param>
        /// <param name="amount">amount</param>
        /// <returns>Transaction Token to make sure</returns>
        private TransactionToken CreateSignAndBroadcastTransaction(AccountModel accFrom, AccountModel accTo, string asset, double amount)
        {
            // ToDo - Create, sign and broadcast
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check if transaction procedure is finished
        /// </summary>
        /// <param name="token">current transaction token</param>
        /// <returns>Null - not ready yeat; True - Done ok; True - Rejected</returns>
        private bool? CheckIfDone(TransactionToken token)
        {
            //ToDo - implement transaction result check
            throw new NotImplementedException();
        }


        private async Task<bool> SendAsync(AccountModel accountFrom, AccountModel accountTo, string assetId, double amount)
        {
            var transactionToken = CreateSignAndBroadcastTransaction(accountFrom, accountTo, assetId, amount);

            var transactionResult = CheckIfDone(transactionToken);

            while (transactionResult == null)
            {
                await Task.Delay(30000);
                transactionResult = CheckIfDone(transactionToken);
            }

            return transactionResult.Value;
        }


        public async Task<bool> ExecuteTaskAsync(AccountModel clinentAcount, AccountModel lykkeAccount, string assetId, double amount)
        {
            if (amount > 0)
                return await SendAsync(lykkeAccount, clinentAcount, assetId, amount);

            if (amount < 0)
                return await SendAsync(clinentAcount, lykkeAccount, assetId, -amount);

            return false;

        }

        public void Execute(TaskToDoDepositWithdraw data, IQueueWriter queueWriter, ILog log)
        {
            Task.Run(async () =>
            {

                try
                {
                    // ToDo - write unit test - if we request Account Model by AccountId and there is not AccountModel can be found
                    var clinetAccount = await _lykkeAccountReader.GetAccountModel(data.ClientPublicAddress);
                    var lykkeAccount = await _lykkeAccountReader.GetAccountModel(LykkeConstats.LykkePublicAddress);
                    var result = await ExecuteTaskAsync(clinetAccount, lykkeAccount, data.AssetId, data.Amount);
                    await queueWriter.WriteQueue(TransactionResultModel.Create(data.TransactionId, result));
                }
                catch (Exception ex)
                {
                    await log.WriteError(GetType().ToString(), "Execute", data.ToJson(), ex);
                    await queueWriter.WriteQueue(TransactionResultModel.Create(data.TransactionId, false));
                }

            });
        }
    }
}
