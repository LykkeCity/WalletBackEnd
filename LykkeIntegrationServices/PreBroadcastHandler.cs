using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.LykkeIntegration.Repositories;
using Core.LykkeIntegration.Services;
using LykkeIntegrationServices.Models.Core.BitCoin;

namespace LykkeIntegrationServices
{
    public class PreBroadcastHandler : IPreBroadcastHandler
    {
        readonly IBitCoinTransactionsRepository _bitCoinTransactionsRepository;
        readonly ICashOperationsRepository _cashOperationsRepository;
        readonly IClientTradesRepository _clientTradesRepository;
        readonly ITransferEventsRepository _transferEventsRepository;

        public PreBroadcastHandler(IBitCoinTransactionsRepository bitCoinTransactionsRepository,
            ICashOperationsRepository cashOperationsRepository,
            IClientTradesRepository clientTradesRepository,
            ITransferEventsRepository transferEventsRepository)
        {
            _bitCoinTransactionsRepository = bitCoinTransactionsRepository;
            _cashOperationsRepository = cashOperationsRepository;
            _clientTradesRepository = clientTradesRepository;
            _transferEventsRepository = transferEventsRepository;

            RegisterHandler("CashIn", HandleCashInAsync);
            RegisterHandler("CashOut", HandleCashOutAsync);
            RegisterHandler("OrdinaryCashOut", HandleOrdinaryCashOutAsync);
            RegisterHandler("Swap", HandleSwapAsync);
            RegisterHandler("Transfer", HandleTransferAsync);
        }

        public async Task<HandleTxError> HandleTx(HandleTxRequest request)
        {
            await HandleOperation(request);
            return null;
        }

        public async Task HandleOperation(HandleTxRequest transactionModel)
        {
            if (_handlers.ContainsKey(transactionModel.Operation))
            {
                await _handlers[transactionModel.Operation](transactionModel);
            }
        }

        public bool CanHandleOperation(string operation)
        {
            return _handlers.ContainsKey(operation);
        }

        private Task HandleCashInAsync(HandleTxRequest transactionModel)
        {
            //tx will be handled by detector
            return Task.FromResult(0);
        }

        private async Task HandleCashOutAsync(HandleTxRequest transactionModel)
        {
            var transactionId = transactionModel.TransactionId;
            var transaction = await _bitCoinTransactionsRepository.FindByTransactionIdAsync(transactionId);
            if (transaction == null)
            {
                return;
            }
            var contextData = transaction.GetContextData<CashInOutContextData>();
            await _cashOperationsRepository.UpdateBlockchainHashAsync(contextData.ClientId, contextData.CashOperationId,
                transactionModel.BlockchainHash);
        }

        private async Task HandleOrdinaryCashOutAsync(HandleTxRequest transactionModel)
        {
            var transaction = await _bitCoinTransactionsRepository.FindByTransactionIdAsync(transactionModel.TransactionId);

            if (transaction == null)
            {
                return;
            }

            var contextData = transaction.GetContextData<CashOutContextData>();

            await _cashOperationsRepository.UpdateBlockchainHashAsync(contextData.ClientId,
                contextData.CashOperationId, transactionModel.BlockchainHash);
        }

        private async Task HandleSwapAsync(HandleTxRequest transactionModel)
        {
            var transactionId = transactionModel.TransactionId;
            var transaction = await _bitCoinTransactionsRepository.FindByTransactionIdAsync(transactionId);
            if (transaction == null)
            {
                return;
            }
            var contextData = transaction.GetContextData<SwapContextData>();
            foreach (var item in contextData.Trades)
            {
                await _clientTradesRepository.UpdateBlockChainHashAsync(item.ClientId, item.TradeId,
                    transactionModel.BlockchainHash);
            }
        }

        private async Task HandleTransferAsync(HandleTxRequest transactionModel)
        {
            var transactionId = transactionModel.TransactionId;
            var transaction = await _bitCoinTransactionsRepository.FindByTransactionIdAsync(transactionId);
            if (transaction == null)
            {
                return;
            }

            var contextData = transaction.GetContextData<TransferContextData>();

            foreach (var transfer in contextData.Transfers)
            {
                try
                {
                    await _transferEventsRepository.UpdateBlockChainHashAsync(transfer.ClientId, transfer.OperationId,
                        transactionModel.BlockchainHash);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
        }

        #region Tools

        private readonly Dictionary<string, Func<HandleTxRequest, Task>> _handlers = new Dictionary<string, Func<HandleTxRequest, Task>>();

        public void RegisterHandler(string operation, Func<HandleTxRequest, Task> handler)
        {
            _handlers.Add(operation, handler);
        }

        #endregion
    }
}
