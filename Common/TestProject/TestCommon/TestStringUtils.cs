using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    [TestClass]
    public class TestStringUtils
    {
        [TestMethod]
        public void TestEmailValidation()
        {
            Assert.IsFalse("sdfsdf".IsValidEmail());
            Assert.IsFalse("Test@Test,tt".IsValidEmail());
            Assert.IsTrue("Test@Test.tt".IsValidEmail());
        }

        [TestMethod]
        public void TestPhonePrefixRemover()
        {
            Assert.AreEqual("9263455496","+79263455496".RemoveCountryPhonePrefix());
            Assert.AreEqual("9263455496", "89263455496".RemoveCountryPhonePrefix());
            Assert.AreEqual("9263455496", "0079263455496".RemoveCountryPhonePrefix());
            Assert.AreEqual("323451", "323451".RemoveCountryPhonePrefix());
        }
    }
}
