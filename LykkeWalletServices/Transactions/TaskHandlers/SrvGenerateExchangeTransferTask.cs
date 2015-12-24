using Core;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvGenerateExchangeTransferTask
    {
        Network network;
        public SrvGenerateExchangeTransferTask(Network network)
        {
            this.network = network;
        }
        public async Task<TaskResultGenerateExchangeTransfer> ExecuteTask(TaskToDoGenerateExchangeTransfer data)
        {
            TaskResultGenerateExchangeTransfer result = new TaskResultGenerateExchangeTransfer();
            if (data.WalletAddress01 == data.WalletAddress02)
            {
                result.HasErrorOccurred = true;
                result.ErrorMessage = "Source and destination wallet addresses could not be the same.";
                result.SequenceNumber = -1;
                return result;
            }

            // Checking the asset amounts
            // ToDo - Alert Unbalanced output is also included
            if (!await OpenAssetsHelper.IsAssetsEnough(data.WalletAddress01, data.Asset01, data.Amount01, network, true))
            {
                result.HasErrorOccurred = true;
                result.ErrorMessage = "Wallet Address " + data.WalletAddress01 + " has not enough of asset " + data.Asset01 + " .";
                result.SequenceNumber = -1;
                return result;
            }
            // ToDo - Alert Unbalanced output is also included
            if (!await OpenAssetsHelper.IsAssetsEnough(data.WalletAddress02, data.Asset02, data.Amount02, network, true))
            {
                result.HasErrorOccurred = true;
                result.ErrorMessage = "Wallet Address " + data.WalletAddress02 + " has not enough of asset " + data.Asset02 + " .";
                result.SequenceNumber = -1;
                return result;
            }

            try
            {
                // ToDo - Check if the following using statement can be done asynchoronously
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    ExchangeRequest req = new ExchangeRequest();
                    req.WalletAddress01 = data.WalletAddress01;
                    req.WalletAddress02 = data.WalletAddress02;
                    req.Asset01 = data.Asset01;
                    req.Asset02 = data.Asset02;
                    req.Amount01 = data.Amount01;
                    req.Amount02 = data.Amount02;
                    req.FirstClientSigned = 0;
                    req.SecondClientSigned = 0;
                    req.ServiceTransactionId = data.TransactionId;
                    req.ExchangeId = Guid.NewGuid().ToString();

                    // ToDo - Create real transactions to be signed
                    TransactionsToBeSigned client01Transaction = new TransactionsToBeSigned();
                    client01Transaction.WalletAddress = data.WalletAddress01;
                    client01Transaction.ExchangeId = req.ExchangeId;
                    client01Transaction.UnsignedTransaction = "";
                    client01Transaction.SignedTransaction = "";
                    entitiesContext.TransactionsToBeSigneds.Add(client01Transaction);

                    TransactionsToBeSigned client02Transaction = new TransactionsToBeSigned();
                    client02Transaction.WalletAddress = data.WalletAddress02;
                    client02Transaction.ExchangeId = req.ExchangeId;
                    client02Transaction.UnsignedTransaction = "";
                    client02Transaction.SignedTransaction = "";
                    entitiesContext.TransactionsToBeSigneds.Add(client02Transaction);

                    entitiesContext.ExchangeRequests.Add(req);
                    await entitiesContext.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                result.HasErrorOccurred = true;
                result.ErrorMessage = e.ToString();
                result.SequenceNumber = -1;
                return result;
            }

            result.SequenceNumber = 0;
            return result;
        }
        public void Execute(TaskToDoGenerateExchangeTransfer data, Func<TaskResultGenerateExchangeTransfer, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
