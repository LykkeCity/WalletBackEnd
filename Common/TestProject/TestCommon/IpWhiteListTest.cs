using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    [TestClass]
    public class IpWhiteListTest
    {
        [TestMethod]
        public void TestMethod1()
        {

            var ipList = new IpWhiteList("127.0.0.1;10.0.0.5");


            var result = ipList.IsIpInList("127.00.0.1:4324");

            Assert.IsTrue(result);


            result = ipList.IsIpInList("127.00.0.2:4324");

            Assert.IsFalse(result);

            result = ipList.IsIpInList("10.0.0.5");

            Assert.IsTrue(result);


            ipList = new IpWhiteList("178.132.86.130");
            Assert.IsTrue(ipList.IsIpInList("178.132.86.130"));

        }
    }
}
