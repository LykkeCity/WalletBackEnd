using System.Threading.Tasks;
using Common;
using Common.Log;
using Core;
using LykkeWalletServices.Transactions.TaskHandlers;
using System;
using NBitcoin;

namespace LykkeWalletServices
{
    public class SrvQueueReader : TimerPeriod
    {
        private readonly ILykkeAccountReader _lykkeAccountReader;
        private readonly IQueueReader _queueReader;
        private readonly IQueueWriter _queueWriter;
        private readonly ILog _log;
        private readonly Network _network;
        private readonly string _exchangePrivateKey;
        private readonly OpenAssetsHelper.AssetDefinition[] _assets;
        private readonly string _rpcUsername = null;
        private readonly string _rpcPassword = null;
        private readonly string _rpcServer = null;

        public SrvQueueReader(ILykkeAccountReader lykkeAccountReader, IQueueReader queueReader, IQueueWriter queueWriter, ILog log,
            Network network, string exchangePrivateKey, OpenAssetsHelper.AssetDefinition[] assets, string rpcUsername,
            string rpcPassword, string rpcServer)
            : base("SrvQueueReader", 5000, log)
        {
            _lykkeAccountReader = lykkeAccountReader;
            _queueReader = queueReader;
            _queueWriter = queueWriter;
            _log = log;
            _network = network;
            _exchangePrivateKey = exchangePrivateKey;
            _assets = assets;
            _rpcUsername = rpcUsername;
            _rpcPassword = rpcPassword;
            _rpcServer = rpcServer;
        }

        protected override async Task Execute()
        {
            var @event = await _queueReader.GetTaskToDo();

            if (@event == null)
                return;

            var transactionGenerateNewWallet = @event as TaskToDoGenerateNewWallet;
            if (transactionGenerateNewWallet != null)
            {
                var service = new SrvGenerateNewWalletTask(_network, _exchangePrivateKey);
                service.Execute(transactionGenerateNewWallet, async result =>
                {
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create(@event.TransactionId, result.Item1, result.Item2));
                });
            }

            var transactionCashIn = @event as TaskToDoCashIn;
            if (transactionCashIn != null)
            {
                var service = new SrvCashInTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer);
                service.Execute(transactionCashIn, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        (@event.TransactionId, result.Item1, result.Item2));
                });
            }

            /*
            var transactionGetBalance = @event as TaskToDoGetBalance;
            if (transactionGetBalance != null)
            {
                var service = new SrvGetBalanceTask();
                service.Execute(transactionGetBalance, async result =>
                {
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create(@event.TransactionId, result));
                });
            }

            var transactionGenerateExchangeTransfer = @event as TaskToDoGenerateExchangeTransfer;
            if (transactionGenerateExchangeTransfer != null)
            {
                var service = new SrvGenerateExchangeTransferTask();
                service.Execute(transactionGenerateExchangeTransfer, async result =>
                {
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create(@event.TransactionId, result));
                });
            }

            var transactionGetTransactionToSign = @event as TaskToDoGetTransactionToSign;
            if (transactionGetTransactionToSign != null)
            {
                var service = new SrvGetTransactionToSignTask();
                service.Execute(transactionGetTransactionToSign, async result =>
                {
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create(@event.TransactionId, result));
                });
            }

            var transactionReturnSignedTransaction = @event as TaskToDoReturnSignedTransaction;
            if (transactionReturnSignedTransaction != null)
            {
                var service = new SrvReturnSignedTransactionTask();
                service.Execute(transactionReturnSignedTransaction, async result =>
                {
                    await _queueWriter.WriteQueue(
                        TransactionResultModel.Create(@event.TransactionId, result));
                });
            }

            var transactionDepositWithdraw = @event as TaskToDoDepositWithdraw;
            if (transactionDepositWithdraw != null)
            {
                var service = new SrvDepositWithdrawTaskHandler(_lykkeAccountReader);
                service.Execute(transactionDepositWithdraw, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create(@event.TransactionId, result));
                },
                    _log);
                return;
            }


            var taskToDoSendAsset = @event as TaskToDoSendAsset;
            if (taskToDoSendAsset != null)
            {
                var service = new SrvExchangeTaskHandler();
                service.Execute(taskToDoSendAsset, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create(@event.TransactionId, result));
                });
                return;
            }
            */
            await _log.WriteWarning("SrvQueueReader", "Execute", "", $"Unknown task type: {@event.GetType()}");

        }
    }
}
