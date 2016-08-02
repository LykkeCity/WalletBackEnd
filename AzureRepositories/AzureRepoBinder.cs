using AzureRepositories.LykkeRepositories;
using AzureStorage.Tables;
using AzureStorage.Tables.Templates.Index;
using Common.IocContainer;
using Common.Log;
using Core;
using Core.LykkeIntegration.Repositories;

namespace AzureRepositories
{
    public static class AzureRepoBinder
    {
        public static void BindAzureRepositories(this IoC ioc, DbSettings dbSettings, ILog log)
        {
            ioc.Register<IBitCoinTransactionsRepository>(
                new BitCoinTransactionsRepository(
                    new AzureTableStorage<BitCoinTransactionEntity>(dbSettings.BitCoinQueueConnectionString,
                        "BitCoinTransactions", log)));

            ioc.Register<ICashOperationsRepository>(
                new CashOperationsRepository(
                    new AzureTableStorage<CashInOutOperationEntity>(dbSettings.ClientPersonalInfoConnString,
                        "OperationsCash", log),
                    new AzureTableStorage<AzureIndex>(dbSettings.ClientPersonalInfoConnString, "OperationsCash", log))
                );

            ioc.Register<IClientTradesRepository>(
                new ClientTradesRepository(
                    new AzureTableStorage<ClientTradeEntity>(dbSettings.HTradesConnString, "Trades", log),
                    new AzureTableStorage<AzureIndex>(dbSettings.ClientPersonalInfoConnString, "Trades", log)));

            ioc.Register<ITransferEventsRepository>(
                new TransferEventsRepository(
                    new AzureTableStorage<TransferEventEntity>(dbSettings.ClientPersonalInfoConnString, "Transfers", log),
                    new AzureTableStorage<AzureIndex>(dbSettings.ClientPersonalInfoConnString, "Transfers", log)));
        }
    }
}
