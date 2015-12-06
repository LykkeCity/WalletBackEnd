using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TcpBus;

namespace TestCommon
{
    [TestClass]
    public class TestTcpBusSerializer
    {
        [TestMethod]
        public async Task TestMethod1()
        {

            var serializer = new TcpBusSerializer();

            var dataToSerialize = new TcpBusData
            {
                Data = "Data",
                Id = "Id",
                Method = "Method",
                ResponseMethod = "ResponseMethod",
                ResponseService = "ResponseService",
                ServiceName = "ServiceName"
            };

            var data = serializer.Serialize(dataToSerialize);

            var ms = new MemoryStream(data);
            ms.Position = 0;


            var recievedData = (TcpBusData)(await serializer.Deserialize(ms)).Item1;

            Assert.AreEqual(dataToSerialize.Data, recievedData.Data);

        }
    }
}
