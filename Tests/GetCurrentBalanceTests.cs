using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using NBitcoin;
using System;

namespace Lykkex.WalletBackend.Tests
{
    public class GetCurrentBalanceModel : BaseRequestModel
    {
        public string MultisigAddress
        {
            get;
            set;
        }

        public int MinimumConfirmation
        {
            get;
            set;
        }
    }

    [TestFixture]
    public class GetCurrentBalanceTests : TaskTestsCommon
    {
        public async static Task<double> GetAssetBalanceForMultisig(string multisig, string assetName, int confirmationNamber,
            AzureStorage.AzureQueueExt QueueReader, AzureStorage.AzureQueueExt QueueWriter)
        {
            GetCurrentBalanceModel getCurrentBalance = new GetCurrentBalanceModel { TransactionId = Guid.NewGuid().ToString(),
                MultisigAddress = multisig, MinimumConfirmation = confirmationNamber };

            var reply = await CreateLykkeWalletRequestAndProcessResult<GetCurrentBalanceResponse>("GetCurrentBalance", getCurrentBalance,
                QueueReader, QueueWriter);

            return reply.Result.ResultArray.Where(item => item.Asset == assetName).FirstOrDefault()?.Amount ?? -1;
        }

        [Test]
        public async Task TestWhenContinuationOfQBitNinjaGetsActive()
        {
            /*
            To test the continution, following fuction in NBitcoin indexer, is modified manually 
            just the Thread.Sleep(25000); is added to introduce delay, after deployment of the indexer dll to QBit.Ninja, for a mass numbered outputs wallet
            continuation flag will be generated as something 105-2add5edcdea771394f14100313792a3f7b9ef45debb35119c17da698231b62e6-82f02cbc4c2fdc9a3ccc8c1b047f87230b6be487ccf5b46a2e6d6a4aec53513d
            The next url to submit is http://localhost:85/balances/2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo?unspentonly=true&colored=true&continuation=105-2add5edcdea771394f14100313792a3f7b9ef45debb35119c17da698231b62e6-82f02cbc4c2fdc9a3ccc8c1b047f87230b6be487ccf5b46a2e6d6a4aec53513d
            
            private IEnumerable<Task<List<OrderedBalanceChange>>> GetOrderedBalanceCoreAsyncOrdered(IEnumerable<List<LoadingTransactionTask>> partitions, CancellationToken cancel)
        {
            Queue<OrderedBalanceChange> unconfirmed = new Queue<OrderedBalanceChange>();
            List<OrderedBalanceChange> unconfirmedList = new List<OrderedBalanceChange>();

            List<OrderedBalanceChange> result = new List<OrderedBalanceChange>();
            foreach(var partition in partitions)
            {
                Thread.Sleep(25000);
                cancel.ThrowIfCancellationRequested();
                var partitionLoading = Task.WhenAll(partition.Select(_ => _.Loaded));
                foreach(var change in partition.Select(p => p.Change))
                {
                    if(change.BlockId == null)
                        unconfirmedList.Add(change);
                    else
                    {
                        if(unconfirmedList != null)
                        {
                            unconfirmed = new Queue<OrderedBalanceChange>(unconfirmedList.OrderByDescending(o => o.SeenUtc));
                            unconfirmedList = null;
                        }

                        while(unconfirmed.Count != 0 && change.SeenUtc < unconfirmed.Peek().SeenUtc)
                        {
                            var unconfirmedChange = unconfirmed.Dequeue();
                            result.Add(unconfirmedChange);
                        }
                        result.Add(change);
                    }
                }
                yield return WaitAndReturn(partitionLoading, result);
                result = new List<OrderedBalanceChange>();
            }
            if(unconfirmedList != null)
            {
                unconfirmed = new Queue<OrderedBalanceChange>(unconfirmedList.OrderByDescending(o => o.SeenUtc));
                unconfirmedList = null;
            }
            while(unconfirmed.Count != 0)
            {
                var change = unconfirmed.Dequeue();
                result.Add(change);
            }
            if(result.Count > 0)
                yield return WaitAndReturn(null, result);
        }
        */

            // The following will generate lots of outputs
            /*
                var multisig = Base58Data.GetFromBase58Data("2NC9qfGybmWgKUdfSebana1HPsAUcXvMmpo")
                    as BitcoinAddress;
                string[] txId = new string[500];
                for (int i = 0; i < txId.Count(); i++)
                {
                    txId[i] = await SendBTC(Settings, multisig, 0.0001f);
                }

                var dummyTxId = await SendBTC(Settings, MassBitcoinHolder, 0.0001f);

                await GenerateBlocks(Settings, 1);
                await WaitUntillQBitNinjaHasIndexed(Settings, HasBalanceIndexed,
                    new string[] { dummyTxId }, multisig.ToWif());
                    */
        }
    }
}
