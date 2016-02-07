using Core;
using LykkeWalletServices.Transactions.Responses;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvGetTransactionToSignTask
    {
        public static async Task<TaskResultGetTransactionToSign> ExecuteTask(TaskToDoGetTransactionToSign data)
        {
            TaskResultGetTransactionToSign result = new TaskResultGetTransactionToSign();
            try
            {
                // ToDo - Check if the following using statement can be done asynchoronously
                using (SqlexpressLykkeEntities entitiesContext = new SqlexpressLykkeEntities())
                {
                    var transactions = await (from transaction in entitiesContext.TransactionsToBeSigneds
                                              where transaction.WalletAddress == data.WalletAddress && (transaction.SignedTransaction == null || transaction.SignedTransaction == string.Empty)
                                              select new { tx = transaction.UnsignedTransaction, eId = transaction.ExchangeId }).ToArrayAsync();


                    if (transactions.Count() != 0)
                    {
                        result.Transactions = new string[transactions.Length];
                        result.ExchangeIds = new string[transactions.Length];
                        for (int i = 0; i < transactions.Length; i++)
                        {
                            result.Transactions[i] = transactions[i].tx;
                            result.ExchangeIds[i] = transactions[i].eId;
                        }
                    }
                    else
                    {
                        result.Transactions = null;
                        result.ExchangeIds = null;
                    }
                }
            }
            catch (Exception e)
            {
                result.HasErrorOccurred = true;
                result.ErrorMessage = e.ToString();
                result.SequenceNumber = -1;
                return result;
            }

            return result;
        }

        public void Execute(TaskToDoGetTransactionToSign data, Func<TaskResultGetTransactionToSign, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
