using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    [TestClass]
    public class TestXmlUtils
    {
        [TestMethod]
        public void TestEscapes()
        {
            const string src = "'My name is Andrey'";
            var result = src.EncodeXmlString();
            Assert.AreEqual("&aposMy name is Andrey&apos", result);
        }

        [TestMethod]
        public void TestNoEscapes()
        {
            const string src = "My name is Andrey";
            var result = src.EncodeXmlString();
            Assert.AreEqual("My name is Andrey", result);
        }
    }
}
