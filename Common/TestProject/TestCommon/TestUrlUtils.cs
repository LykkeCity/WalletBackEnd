using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{

    [TestClass]
    public class TestUrlUtils
    {
        public class TestModel
        {
            public string A { get; set; }
            public string B { get; set; }

        }


        [TestMethod]
        public void TestMakingUrl()
        {
            var model = new TestModel
            {
                A = "TestA",
                B = "TestB"
            };
            var urlString = UrlUtils.ToUrlParamString(model);
            Assert.AreEqual("a=TestA&b=TestB", urlString);
        }

        [TestMethod]
        public void TestMakingUrlWithIgnore()
        {
            var model = new TestModel
            {
                A = "TestA",
                B = "TestB"
            };
            var urlString = UrlUtils.ToUrlParamString(model, "a");
            Assert.AreEqual("b=TestB", urlString);
        }
    }
}
