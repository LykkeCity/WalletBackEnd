using System.Threading.Tasks;
using Common;
using Core;

namespace AzureRepositories
{
    public class AzureQueueReader : IQueueReader
    {
        private readonly IQueueExt _queueExt;

        public AzureQueueReader(IQueueExt queueExt)
        {
            _queueExt = queueExt;

            _queueExt.RegisterTypes(
                QueueType.Create("GenerateNewWallet", typeof(TaskToDoGenerateNewWallet)),
                QueueType.Create("CashIn", typeof(TaskToDoCashIn)),
                QueueType.Create("OrdinaryCashIn", typeof(TaskToDoOrdinaryCashIn)),
                QueueType.Create("CashOut", typeof(TaskToDoCashOut)),
                QueueType.Create("CashOutSeparateSignatures", typeof(TaskToDoCashOutSeparateSignatures)),
                QueueType.Create("OrdinaryCashOut", typeof(TaskToDoOrdinaryCashOut)),
                QueueType.Create("GetCurrentBalance", typeof(TaskToDoGetCurrentBalance)),
                QueueType.Create("Swap", typeof(TaskToDoSwap)),
                QueueType.Create("GetBalance", typeof(TaskToDoGetBalance)),
                QueueType.Create("DepositWithdraw", typeof(TaskToDoDepositWithdraw)),
                QueueType.Create("Exchange", typeof(TaskToDoSendAsset)),
                QueueType.Create("GenerateFeeOutputs", typeof(TaskToDoGenerateFeeOutputs)),
                QueueType.Create("GenerateIssuerOutputs", typeof(TaskToDoGenerateIssuerOutputs)),
                QueueType.Create("Transfer", typeof(TaskToDoTransfer)),
                QueueType.Create("TransferAllAssetsToAddress", typeof(TaskToDoTransferAllAssetsToAddress)),
                QueueType.Create("GetIssuersOutputStatus", typeof(TaskToDoGetIssuersOutputStatus)),
                QueueType.Create("GetFeeOutputsStatus", typeof(TaskToDoGetFeeOutputsStatus)),
                QueueType.Create("GenerateRefundingTransaction", typeof(TaskToDoGenerateRefundingTransaction)),
                QueueType.Create("GetInputWalletAddresses", typeof(TaskToDoGetInputWalletAddresses)),
                QueueType.Create("UpdateAssets", typeof(TaskToDoUpdateAssets)),
                QueueType.Create("GetExpiredUnclaimedRefundingTransactions", typeof(TaskToDoGetExpiredUnclaimedRefundingTransactions))
                );
        }

        public async Task<TransactionToDoBase> GetTaskToDo()
        {
            // ToDo - improve - make sure that we shold Delete it only after we put it to our Local Storage. For first stage is good
            var message = await _queueExt.GetMessageAsync();
            return message as TransactionToDoBase;
        }

    }
}
