﻿using Core;
using NBitcoin;
using NBitcoin.OpenAsset;
using System;
using System.Threading.Tasks;

namespace LykkeWalletServices.Transactions.TaskHandlers
{
    // Sample Input: CashOutSeparateSignatures:{"TransactionId":"10","MultisigAddress":"2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo","Amount":200,"Currency":"bjkUSD","PrivateKey":""}
    // Sample output: CashOutSeparateSignatures:{"TransactionId":"10","Result":{"TransactionHex":"xxx","TransactionHash":"xxx"},"Error":null}
    // There would be two type of mixing singnature on a transaction
    // 1- Mixing signature on a signle input (the multisig)
    // 2- Mixing signatures on different signed inputs to form a final transaction
    // This class handles the first scenario
    public class SrvCashOutSeparateSignaturesTask : SrvNetworkInvolvingExchangeBase
    {
        public SrvCashOutSeparateSignaturesTask(Network network, AssetDefinition[] assets, string username,
            string password, string ipAddress, string feeAddress, string connectionString) : base(network, assets, username, password, ipAddress, feeAddress, connectionString)
        {
        }

        // This is the function which will be executed on each device (being it client or exchange),
        // this should be same for both client and exchange, the secret is available on the device
        // If device is informed about multisigAddress and amount and currency (which is not secret,
        // and specifies the transaction to be built),
        // the other parameters should be made available
        // ToDo: Another approach would be to use .Net reflectin to pass the code to be executed, not
        // to have the problem to make both side sync
        private static async Task<Tuple<string, Error>> GenerateUncompleteTransactionWithOnlyOneSignature(string multisigAddress, double amount, string currency,
            AssetDefinition[] assets, OpenAssetsHelper.RPCConnectionParams connectionParams, string connectionString,
            BitcoinSecret secret)
        {
            using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(connectionString))
            {
                OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(multisigAddress,0, amount, currency,
                        assets, connectionParams, connectionString, entities, false);
                if (walletCoins.Error != null)
                {
                    return new Tuple<string, Error>(null, walletCoins.Error);
                }

                TransactionBuilder builder = new TransactionBuilder();
                var tx = builder
                .AddCoins(walletCoins.ScriptCoins)
                .AddCoins(walletCoins.AssetScriptCoins)
                .AddKeys(secret)
                .SendAsset(walletCoins.Asset.AssetAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((amount * walletCoins.Asset.AssetMultiplicationFactor))))
                .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork))
                .BuildTransaction(true);
                return new Tuple<string, Error>(tx.ToHex(), null);
            }
        }

        public async Task<Tuple<CashOutSeparateSignaturesTaskResult, Error>> ExecuteTask(TaskToDoCashOutSeparateSignatures data)
        {
            CashOutSeparateSignaturesTaskResult result = null;
            Error error = null;

            try
            {
                using (SqlexpressLykkeEntities entities = new SqlexpressLykkeEntities(ConnectionString))
                {
                    var clientAddress = await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities);
                    var txSignedByClient = await GenerateUncompleteTransactionWithOnlyOneSignature(data.MultisigAddress, data.Amount, data.Currency,
                        Assets, connectionParams, ConnectionString, new BitcoinSecret(clientAddress.WalletPrivateKey));
                    if (txSignedByClient.Item2 != null)
                    {
                        error = txSignedByClient.Item2;
                    }
                    else
                    {
                        var multisig = await OpenAssetsHelper.GetMatchingMultisigAddress(data.MultisigAddress, entities);
                        var txSignedByExchange = await GenerateUncompleteTransactionWithOnlyOneSignature(data.MultisigAddress, data.Amount, data.Currency,
                            Assets, connectionParams, ConnectionString, (new BitcoinSecret(multisig.WalletPrivateKey)));
                        if (txSignedByExchange.Item2 != null)
                        {
                            error = txSignedByExchange.Item2;
                        }

                        OpenAssetsHelper.GetScriptCoinsForWalletReturnType walletCoins = 
                            (OpenAssetsHelper.GetScriptCoinsForWalletReturnType)await OpenAssetsHelper.GetCoinsForWallet(data.MultisigAddress,0, data.Amount, data.Currency,
                            Assets, connectionParams, ConnectionString, entities, false);
                        if (walletCoins.Error != null)
                        {
                            error = walletCoins.Error;
                        }
                        else
                        {
                            TransactionBuilder builder = new TransactionBuilder();
                            var txCommon = builder
                                .AddCoins(walletCoins.ScriptCoins)
                                .AddCoins(walletCoins.AssetScriptCoins)
                                .SendAsset(walletCoins.Asset.AssetAddress, new AssetMoney(new AssetId(new BitcoinAssetId(walletCoins.Asset.AssetId, connectionParams.BitcoinNetwork)), Convert.ToInt64((data.Amount * walletCoins.Asset.AssetMultiplicationFactor))))
                                .SendFees(new Money(OpenAssetsHelper.TransactionSendFeesInSatoshi))
                                .SetChange(new Script(walletCoins.MatchingAddress.MultiSigScript).GetScriptAddress(connectionParams.BitcoinNetwork))
                                .BuildTransaction(true);
                            Transaction tx = builder.CombineSignatures(new Transaction[] { txCommon, new Transaction(txSignedByClient.Item1), new Transaction(txSignedByExchange.Item1) });
                            if (!builder.Verify(tx))
                            {
                                error = new Error();
                                error.Code = ErrorCode.TransactionNotSignedProperly;
                                error.Message = "Two bodies have signed the transaction (exchange and the client), their signature could not be mixed properly.";
                            }
                            else
                            {
                                Error localerror = null;
                                using (var transaction = entities.Database.BeginTransaction())
                                {
                                    var txHash = tx.GetHash().ToString();
                                    
                                    localerror = (await OpenAssetsHelper.CheckTransactionForDoubleSpentThenSendIt
                                        (tx, connectionParams, entities, ConnectionString, null, null)).Error;


                                    if (localerror == null)
                                    {
                                        result = new CashOutSeparateSignaturesTaskResult
                                        {
                                            TransactionHex = tx.ToHex(),
                                            TransactionHash = txHash
                                        };
                                    }
                                    else
                                    {
                                        error = localerror;
                                    }

                                    if (error == null)
                                    {
                                        transaction.Commit();
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = new Error();
                error.Code = ErrorCode.Exception;
                error.Message = e.ToString();
            }

            return new Tuple<CashOutSeparateSignaturesTaskResult, Error>(result, error);
        }

        public void Execute(TaskToDoCashOutSeparateSignatures data,
            Func<Tuple<CashOutSeparateSignaturesTaskResult, Error>, Task> invokeResult)
        {
            Task.Run(async () =>
            {
                var result = await ExecuteTask(data);
                await invokeResult(result);
            });
        }
    }
}
