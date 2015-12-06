using System;
using System.Web;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    public class TestParams
    {
        public string A { get; set; }
        public int B { get; set; }

    }

    [TestClass]
    public class TestUtils
    {
        [TestMethod]
        public void TestParamsToString()
        {
            var result = Utils.ParamsToString(new TestParams {A = "test", B = 14});
            Assert.AreEqual("A=[test];B=[14];", result);
        }

        [TestMethod]
        public void TestNaveValueCollection()
        {
            var data = HttpUtility.ParseQueryString(
                "Type=MoneyDeposit&retCode=0&TransactionID=123456789&Amount=500&Installments=1&Currency=EUR&BankAmount=500.00&BankCurrency=EUR&key=1235");

            Assert.AreEqual("MoneyDeposit", data["tYpe"]);

            Assert.AreEqual(null, data["tYpea"]);
        }

    }


}
