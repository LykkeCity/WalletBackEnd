using Core;
using LykkeWalletServices.Transactions.Responses;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    public class SrvReturnSignedTransactionTask
    {
        Network network;

        long multiplicationFactor = 1;
        public SrvReturnSignedTransactionTask(Network network, long multiplicationFactor)
        {
            this.network = network;
            this.multiplicationFactor = multiplicationFactor;
        }
        public async Task<TaskResultReturnSignedTransaction> ExecuteTask(TaskToDoReturnSignedTransaction data)
        {
            TaskResultReturnSignedTransaction result = new TaskResultReturnSignedTransaction();
            try
            {
                // ToDo - Check if the following using statement can be done asynchoronously
                using (SqliteLykkeServicesEntities entitiesContext = new SqliteLykkeServicesEntities())
                {
                    var transaction = await (from t in entitiesContext.TransactionsToBeSigneds
                                             where t.WalletAddress == data.WalletAddress && t.ExchangeId == data.ExchangeId
                                             select t).FirstAsync();

                    if (transaction == null)
                    {
                        result.HasErrorOccurred = true;
                        result.ErrorMessage = "Could not find the transaction which the signed version has been submitted.";
                        result.SequenceNumber = -1;
                        return result;
                    }

                    var exchangeTransaction = await (from et in entitiesContext.ExchangeRequests
                                                     where et.ExchangeId == data.ExchangeId
                                                     select et).FirstAsync();
                    if (exchangeTransaction == null)
                    {
                        result.HasErrorOccurred = true;
                        result.ErrorMessage = "Could not find the exchange request which the transaction belonged to.";
                        result.SequenceNumber = -1;
                        return result;
                    }
                    if (exchangeTransaction.WalletAddress01 != data.WalletAddress && exchangeTransaction.WalletAddress02 != data.WalletAddress)
                    {
                        result.HasErrorOccurred = true;
                        result.ErrorMessage = "Could not find the proper exchange request which the transaction belonged to. One with invalid wallet addresses found.";
                        result.SequenceNumber = -1;
                        return result;
                    }

                    if (exchangeTransaction.WalletAddress01 == data.WalletAddress)
                    {
                        exchangeTransaction.FirstClientSigned = 1;
                    }
                    else
                    {
                        exchangeTransaction.SecondClientSigned = 1;
                    }
                    transaction.SignedTransaction = data.SignedTransaction;
                    await entitiesContext.SaveChangesAsync();

                    if (exchangeTransaction.FirstClientSigned == 1 && exchangeTransaction.SecondClientSigned == 1)
                    {
                        var tx = await GenerateTransactionHex(data.ExchangeId, entitiesContext, 
                            network, multiplicationFactor);


                        var transactions = await (from t in entitiesContext.TransactionsToBeSigneds
                                                  where t.ExchangeId == data.ExchangeId
                                                  select t).ToArrayAsync();

                        entitiesContext.TransactionsToBeSigneds.RemoveRange(transactions);
                        entitiesContext.ExchangeRequests.Remove(exchangeTransaction);
                    }

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

            return result;
        }

        // ToDo - Work on the real implementation, in the current implementation really transactions are not mixed,
        // the private keys are got from the clients and the trasaction is created here.
        /// <summary>
        /// Generates transaction hex from the provicded parameters
        /// </summary>
        /// <param name="exchangeId">Id of the exchange transaction to be built.</param>
        /// <param name="entitiesContext">The database context to read the transaction data from.</param>
        /// <returns>The transaction hex made for the exchange transaction.</returns>
        private static async Task<string> GenerateTransactionHex(string exchangeId, SqliteLykkeServicesEntities entitiesContext, 
            Network network, long multiplicationFactor)
        {
            var transaction = await (from tx in entitiesContext.ExchangeRequests
                                     where tx.ExchangeId == exchangeId
                                     select tx).FirstAsync();

            var wallet01PrivateKey = await (from w in entitiesContext.TransactionsToBeSigneds
                                            where w.ExchangeId == exchangeId && w.WalletAddress == transaction.WalletAddress01
                                            select w.SignedTransaction).FirstAsync();

            var wallet02PrivateKey = await (from w in entitiesContext.TransactionsToBeSigneds
                                            where w.ExchangeId == exchangeId && w.WalletAddress == transaction.WalletAddress02
                                            select w.SignedTransaction).FirstAsync();


            // ToDo - Alert Unbalanced output is also included
            if (!(await OpenAssetsHelper.IsAssetsEnough(transaction.WalletAddress01,
                transaction.Asset01, (int)transaction.Amount01, network, multiplicationFactor, true)))
            {
                throw new Exception("Not sufficient funds for asset: " + transaction.Asset01 +
                    " in wallet: " + transaction.WalletAddress01);
            }
            // ToDo - Alert Unbalanced output is also included
            if (!(await OpenAssetsHelper.IsAssetsEnough(transaction.WalletAddress02,
                transaction.Asset02, (int)transaction.Amount02, network, multiplicationFactor, true)))
            {
                throw new Exception("Not sufficient funds for asset: " + transaction.Asset02 +
                    " in wallet: " + transaction.WalletAddress02);
            }

            var wallet01Outputs = await OpenAssetsHelper.GetWalletOutputs(transaction.WalletAddress01, network);
            if (wallet01Outputs.Item2)
            {
                throw new Exception("Could not get the wallet available outputs. The actual error message is: " + wallet01Outputs.Item3);
            }

            var wallet02Outputs = await OpenAssetsHelper.GetWalletOutputs(transaction.WalletAddress02, network);
            if (wallet02Outputs.Item2)
            {
                throw new Exception("Could not get the wallet available outputs. The actual error message is: " + wallet02Outputs.Item3);
            }

            // ToDo - Fix the credentials, probably it will be removed entirely
            var wallet01Coins = await OpenAssetsHelper.GetColoredUnColoredCoins
                (wallet01Outputs.Item1, transaction.Asset01, network, null, null, null);
            var wallet02Coins = await OpenAssetsHelper.GetColoredUnColoredCoins
                (wallet02Outputs.Item1, transaction.Asset02, network, null, null, null);
                
            /*
            var wallet01AssetOutputs = OpenAssetsHelper.GetWalletOutputsForAsset(wallet01Outputs.Item1, transaction.Asset01);
            var wallet01UncoloredOutputs = OpenAssetsHelper.GetWalletOutputsUncolored(wallet01Outputs.Item1);
            var wallet01ColoredTransactions = await GetTransactionsHex(wallet01AssetOutputs);
            var wallet01UncoloredTransactions = await GetTransactionsHex(wallet01AssetOutputs);
            var wallet01ColoredCoins = GenerateWalletColoredCoins(wallet01ColoredTransactions, wallet01AssetOutputs, transaction.Asset01);
            var wallet01UncoloredCoins = GenerateWalletUnColoredCoins(wallet01UncoloredTransactions, wallet01UncoloredOutputs);
            */


            /*
            var wallet02Outputs = await OpenAssetsHelper.GetWalletOutputs(transaction.WalletAddress02);

            var wallet01Transactions = await GetTransactionsHex(wallet01Outputs);
            var wallet02Transactions = await GetTransactionsHex(wallet02Outputs);

            var wallet01ColoredCoins = GenerateWalletColoredCoins(wallet01Transactions, wallet01Outputs, transaction.Asset01);
            var wallet02ColoredCoins = GenerateWalletColoredCoins(wallet02Transactions, wallet02Outputs, transaction.Asset02);

            var wallet01UnColoredCoins = GenerateWalletUnColoredCoins(wallet01Transactions, wallet01Outputs);
            var wallet02UnColoredCoins = GenerateWalletUnColoredCoins(wallet02Transactions, wallet02Outputs);
            */
            // Building the exchange transaction
            ColorMarker.Tag = ColorMarker.OATag;
            TransactionBuilder txBuilder = new TransactionBuilder();
            var txToBroadcast = txBuilder
                .AddCoins(wallet01Coins.Item1)
                .AddCoins(wallet01Coins.Item2)
                .AddKeys(new BitcoinSecret(wallet01PrivateKey))
                .SendAsset(new BitcoinAddress(transaction.WalletAddress02), new AssetMoney(new AssetId(new BitcoinAssetId(transaction.Asset01)), (long)transaction.Amount01))
                .SetChange(new BitcoinAddress(transaction.WalletAddress01))
                .Then()
                .AddCoins(wallet02Coins.Item1)
                .AddCoins(wallet02Coins.Item2)
                .AddKeys(new BitcoinSecret(wallet02PrivateKey))
                .SendAsset(new BitcoinAddress(transaction.WalletAddress01), new AssetMoney(new AssetId(new BitcoinAssetId(transaction.Asset02)), (long)transaction.Amount02))
                .SetChange(new BitcoinAddress(transaction.WalletAddress02))
                .BuildTransaction(true);

            return txToBroadcast.ToHex();
        }

        

        public void Execute(TaskToDoReturnSignedTransaction data, Func<TaskResultReturnSignedTransaction, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
