using System;
using System.Runtime.Serialization;
using System.Threading;
using AzureStorage;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    [DataContract]
    public class TestClass
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public DateTime DateTime { get; set; }
    }

    [TestClass]
    public class QueueTest
    {
        [TestMethod]
        public void PutAndPeek()
        {
            var queue = new AzureQueue<TestClass>("UseDevelopmentStorage=true", "TestQueue");
            queue.Clear();
            var putClass = new TestClass {DateTime = DateTime.UtcNow, Description = "Test Description", Id = 15};
            queue.PutMessage(putClass);

            var msg = queue.GetMessage();

            Assert.AreEqual(putClass.Id, msg.Id);
            Assert.AreEqual(putClass.Description, msg.Description);

        }


        private AzureQueue<TestClass> _asyncQueue;
        private readonly TestClass _putClass = new TestClass {Description = "ttt", Id=15,DateTime = DateTime.UtcNow};
        private bool _asyncRead = false;

        private async void TestAsync()
        {
            var msg = await _asyncQueue.GetMessageAsync();

            Assert.AreEqual(_putClass.Id, msg.Id);
            Assert.AreEqual(_putClass.Description, msg.Description);

            _asyncRead = true;
        }

        [TestMethod]
        public void AsyncPeek()
        {
            _asyncQueue = new AzureQueue<TestClass>("UseDevelopmentStorage=true", "TestQueue");
            _asyncQueue.Clear();

            var thread = new Thread(TestAsync);
            thread.Start();

            _asyncQueue.PutMessage(_putClass);
            while (!_asyncRead)
            {
                Thread.Sleep(100);
            }
        }
    }
}
