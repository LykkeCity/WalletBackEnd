using Core;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample request: GetExpiredUnclaimedRefundingTransactions:{"TransactionId":"10","MultisigAddress":"2Mz5iEcM7VT3aaGRhKaAdRzJtRJtDKoYMsL"}
    // Sample response: GetExpiredUnclaimedRefundingTransactions:{"TransactionId":"10","Result":{"Elements":[{"TxId":"xxx","TxHex":"xxx"}]},"Error":null}
    public class SrvGetExpiredUnclaimedRefundingTransactionsTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvGetExpiredUnclaimedRefundingTransactionsTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string feePrivateKey, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
        }

        public async Task<Tuple<GetExpiredUnclaimedRefundingTransactionsTaskResult, Error>> ExecuteTask(TaskToDoGetExpiredUnclaimedRefundingTransactions data)
        {
            GetExpiredUnclaimedRefundingTransactionsTaskResult result = null;
            Error error = null;
            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    DateTime timeout = DateTime.UtcNow.AddMinutes(OpenAssetsHelper.LocktimeMinutesAllowance);
                    var txIds = entities.RefundedOutputs
                        .Where(r => !(r.LockTime >= timeout) && (data.MultisigAddress != null ? r.RefundedAddress == data.MultisigAddress : true))
                        .GroupBy(o => o.RefundTxId).
                        Select(g => new { Key = g.Key, Value = g.Sum(r => r.HasBeenSpent ? 1 : 0) }).Where(g => g.Value == 0).Select(g => g.Key);

                    var records = entities.RefundTransactions.Where(r => txIds.Contains(r.id))
                        .Select(r => new GetExpiredUnclaimedRefundingTransactionsTaskResultElement { TxId = r.RefundTxId, TxHex = r.RefundTxHex });

                    IList<GetExpiredUnclaimedRefundingTransactionsTaskResultElement> retList =
                        new List<GetExpiredUnclaimedRefundingTransactionsTaskResultElement>();
                    foreach(var tx in records)
                    {
                        var txHexResult = await OpenAssetsHelper.GetTransactionHex
                            (tx.TxId, connectionParams);
                        if (txHexResult.Item1)
                        {
                            // In case of error, we consider it unbroadcasted, since we do not know actually it is or not
                            retList.Add(tx);
                        }
                    }

                    result = new GetExpiredUnclaimedRefundingTransactionsTaskResult
                    {
                        Elements = retList.ToArray()
                    };
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }
            return new Tuple<GetExpiredUnclaimedRefundingTransactionsTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoGetExpiredUnclaimedRefundingTransactions data, Func<Tuple<GetExpiredUnclaimedRefundingTransactionsTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
