using Common;
using Common.Log;
using Core;
using LykkeWalletServices.Transactions.TaskHandlers;
using NBitcoin;
using System;
using System.Threading.Tasks;
using Common.IocContainer;
using Core.LykkeIntegration.Services;

namespace LykkeWalletServices
{
    public class SrvQueueReader : TimerPeriod
    {
        private readonly ILykkeAccountReader _lykkeAccountReader;
        private readonly IQueueReader _queueReader;
        private readonly IQueueWriter _queueWriter;
        private readonly ILog _log;
        private readonly Network _network;
        public AssetDefinition[] _assets
        {
            get;
            set;
        }
        private readonly string _rpcUsername = null;
        private readonly string _rpcPassword = null;
        private readonly string _rpcServer = null;
        private readonly string _connectionString = null;
        private readonly string _feeAddress;
        private readonly string _feeAddressPrivateKey;
        private readonly IPreBroadcastHandler _preBroadcastHandler;

        public SrvQueueReader(IQueueReader queueReader, IQueueWriter queueWriter, ILog log,
            Network network, AssetDefinition[] assets, string rpcUsername,
            string rpcPassword, string rpcServer, string connectionString, string feeAddress, string feeAddressPrivateKey, IPreBroadcastHandler preBroadcastHandler)
            : base("SrvQueueReader", 5000, log)
        {
            _queueReader = queueReader;
            _queueWriter = queueWriter;
            _log = log;
            _network = network;
            _assets = assets;
            _rpcUsername = rpcUsername;
            _rpcPassword = rpcPassword;
            _rpcServer = rpcServer;
            _connectionString = connectionString;
            _feeAddress = feeAddress;
            _feeAddressPrivateKey = feeAddressPrivateKey;
            _preBroadcastHandler = preBroadcastHandler;
        }

        private async Task ProcessTaskResult(TransactionToDoBase @event, TransactionResultModel resultModel)
        {
            await _queueWriter.WriteQueue(resultModel);

            try
            {
                string inputMessage = @event.ToJson();
                string outputMessage = resultModel.OperationName + ":" +
                    Newtonsoft.Json.JsonConvert.SerializeObject(resultModel);
                await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log, inputMessage, outputMessage);
            }
            catch(Exception e)
            {
                await _log.WriteFatalError("SrvQueueReader", string.Empty, string.Empty, e,
                    DateTime.UtcNow);
            }
        }

        protected override async Task Execute()
        {
            bool knownTaskType = false;

            var @event = await _queueReader.GetTaskToDo();

            if (@event == null)
                return;

            var transactionGenerateNewWallet = @event as TaskToDoGenerateNewWallet;
            if (transactionGenerateNewWallet != null)
            {
                var service = new SrvGenerateNewWalletTask(_network, _connectionString);
                service.Execute(transactionGenerateNewWallet, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create("GenerateNewWallet", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event,
                        TransactionResultModel.Create("GenerateNewWallet", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionCashIn = @event as TaskToDoCashIn;
            if (transactionCashIn != null)
            {
                var service = new SrvCashInTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _connectionString, _feeAddress, _preBroadcastHandler);
                service.Execute(transactionCashIn, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("CashIn", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("CashIn", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionOrdinaryCashIn = @event as TaskToDoOrdinaryCashIn;
            if (transactionOrdinaryCashIn != null)
            {
                var service = new SrvOrdinaryCashInTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _connectionString, _feeAddress, _preBroadcastHandler);
                service.Execute(transactionOrdinaryCashIn, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("OrdinaryCashIn", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("OrdinaryCashIn", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionCashOut = @event as TaskToDoCashOut;
            if (transactionCashOut != null)
            {
                var service = new SrvCashOutTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _feeAddress, _connectionString, _preBroadcastHandler);
                service.Execute(transactionCashOut, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("CashOut", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("CashOut", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionUncolor = @event as TaskToDoUncolor;
            if (transactionUncolor != null)
            {
                var service = new SrvUncolorTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _feeAddress, _connectionString, _preBroadcastHandler);
                service.Execute(transactionUncolor, async result =>
                {
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("Uncolor", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionCashOutSeparateSignatures = @event as TaskToDoCashOutSeparateSignatures;
            if (transactionCashOutSeparateSignatures != null)
            {
                var service = new SrvCashOutSeparateSignaturesTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer, _feeAddress, _connectionString);
                service.Execute(transactionCashOutSeparateSignatures, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("CashOutSeparateSignatures", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("CashOutSeparateSignatures", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionOrdinaryCashOut = @event as TaskToDoOrdinaryCashOut;
            if (transactionOrdinaryCashOut != null)
            {
                var service = new SrvOrdinaryCashOutTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer, _feeAddress,
                    _connectionString, _preBroadcastHandler);
                service.Execute(transactionOrdinaryCashOut, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("OrdinaryCashOut", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("OrdinaryCashOut", @event.TransactionId, result.Item1, result.Item2));
                   
                });
                knownTaskType = true;
            }

            var transactionGetCurrentBalance = @event as TaskToDoGetCurrentBalance;
            if (transactionGetCurrentBalance != null)
            {
                var service = new SrvGetCurrentBalanceTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _connectionString, _feeAddress);
                service.Execute(transactionGetCurrentBalance, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GetCurrentBalance", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GetCurrentBalance", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionSwap = @event as TaskToDoSwap;
            if (transactionSwap != null)
            {
                var service = new SrvSwapTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _feeAddress, _connectionString, _preBroadcastHandler);
                service.Execute(transactionSwap, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("Swap", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("Swap", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionRechargeFeeWallet = @event as TaskToDoGenerateFeeOutputs;
            if (transactionRechargeFeeWallet != null)
            {
                var service = new SrvGenerateFeeOutputsTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _connectionString, _feeAddress, _feeAddressPrivateKey);
                service.Execute(transactionRechargeFeeWallet, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GenerateFeeOutputs", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GenerateFeeOutputs", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionRechargeIssuerWallet = @event as TaskToDoGenerateIssuerOutputs;
            if (transactionRechargeIssuerWallet != null)
            {
                var service = new SrvGenerateIssuerOutputsTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _connectionString);
                service.Execute(transactionRechargeIssuerWallet, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GenerateIssuerOutputs", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GenerateIssuerOutputs", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionTransfer = @event as TaskToDoTransfer;
            if (transactionTransfer != null)
            {
                var service = new SrvTransferTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _feeAddress, _connectionString, _preBroadcastHandler);
                service.Execute(transactionTransfer, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("Transfer", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    *////
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("Transfer", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionTransferAllAssetsToAddress = @event as TaskToDoTransferAllAssetsToAddress;
            if (transactionTransferAllAssetsToAddress != null)
            {
                var service = new SrvTransferAllAssetsToAddressTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _feeAddress, _connectionString, _preBroadcastHandler);
                service.Execute(transactionTransferAllAssetsToAddress, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("Transfer", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    *////
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("TransferAllAssetsToAddress", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionGetIssuerOutputStatus = @event as TaskToDoGetIssuersOutputStatus;
            if (transactionGetIssuerOutputStatus != null)
            {
                var service = new SrvGetIssuersOutputStatusTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _connectionString, _feeAddress);
                service.Execute(transactionGetIssuerOutputStatus, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GetIssuersOutputStatus", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GetIssuersOutputStatus", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionGetFeeOutputsStatus = @event as TaskToDoGetFeeOutputsStatus;
            if (transactionGetFeeOutputsStatus != null)
            {
                var service = new SrvGetFeeOutputsStatusTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _connectionString, _feeAddress);
                service.Execute(transactionGetFeeOutputsStatus, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GetFeeOutputsStatus", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GetFeeOutputsStatus", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionGenerateRefundingTransaction = @event as TaskToDoGenerateRefundingTransaction;
            if (transactionGenerateRefundingTransaction != null)
            {
                var service = new SrvGenerateRefundingTransactionTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _feeAddress, _feeAddressPrivateKey, _connectionString);
                service.Execute(transactionGenerateRefundingTransaction, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GenerateRefundingTransaction", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GenerateRefundingTransaction", @event.TransactionId, result.Item1, result.Item2));
                     
                });
                knownTaskType = true;
            }

            var transactionGetExpiredUnclaimedRefundingTransactions = @event as TaskToDoGetExpiredUnclaimedRefundingTransactions;
            if (transactionGetExpiredUnclaimedRefundingTransactions != null)
            {
                var service = new SrvGetExpiredUnclaimedRefundingTransactionsTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer,
                    _feeAddress, _feeAddressPrivateKey, _connectionString);
                service.Execute(transactionGetExpiredUnclaimedRefundingTransactions, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("GetExpiredUnclaimedRefundingTransactions", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("GetExpiredUnclaimedRefundingTransactions", @event.TransactionId, result.Item1, result.Item2));
                     
                });
                knownTaskType = true;
            }

            var transactionUpdateAssets = @event as TaskToDoUpdateAssets;
            if (transactionUpdateAssets != null)
            {
                var service = new SrvUpdateAssetsTask(this);
                service.Execute(transactionUpdateAssets, async result =>
                {
                    /*
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        ("UpdateAssets", @event.TransactionId, result.Item1, result.Item2));
                    await OpenAssetsHelper.PerformFunctionEndJobs(_connectionString, _log);
                    */
                    await ProcessTaskResult(@event, TransactionResultModel.Create
                        ("UpdateAssets", @event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            if (knownTaskType)
            {
                await _log.WriteWarning("SrvQueueReader", "Execute", "", $"{@event.GetType()}");
            }
            else
            {
                await _log.WriteWarning("SrvQueueReader", "Execute", "", $"Unknown task type: {@event.GetType()}");
            }
        }
    }
}
