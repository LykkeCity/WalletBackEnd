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
            bool knownTaskType = false;

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
                knownTaskType = true;
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
                knownTaskType = true;
            }

            var transactionCashOut = @event as TaskToDoCashOut;
            if (transactionCashOut != null)
            {
                var service = new SrvCashOutTask(_network, _assets, _rpcUsername, _rpcPassword, _rpcServer, _exchangePrivateKey);
                service.Execute(transactionCashOut, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        (@event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionGetCurrentBalance = @event as TaskToDoGetCurrentBalance;
            if (transactionGetCurrentBalance != null)
            {
                var service = new SrvGetCurrentBalanceTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer);
                service.Execute(transactionGetCurrentBalance, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        (@event.TransactionId, result.Item1, result.Item2));
                });
                knownTaskType = true;
            }

            var transactionSwap = @event as TaskToDoSwap;
            if (transactionSwap != null)
            {
                var service = new SrvSwapTask(_network, _assets, _rpcUsername,
                    _rpcPassword, _rpcServer, _exchangePrivateKey);
                service.Execute(transactionSwap, async result =>
                {
                    await _queueWriter.WriteQueue(TransactionResultModel.Create
                        (@event.TransactionId, result.Item1, result.Item2));
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
