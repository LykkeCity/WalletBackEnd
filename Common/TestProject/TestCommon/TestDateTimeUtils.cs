using System;
using Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestCommon
{
    /// <summary>
    /// Summary description for TestDateTimeUtils
    /// </summary>
    [TestClass]
    public class TestDateTimeUtils
    {
        public class TestWeeklyPeriod : IWeeklyPeriod
        {
            public DayOfWeek DayOfWeekFrom { get; set; }
            public string TimeFrom { get; set; }
            public DayOfWeek DayOfWeekTo { get; set; }
            public string TimeTo { get; set; }
        }

        [TestMethod]
        public void FromGreaterThanTo()
        {
            var testPeriod = new TestWeeklyPeriod
            {
                DayOfWeekFrom = DayOfWeek.Friday, 
                TimeFrom = "20:55:00", 
                DayOfWeekTo = DayOfWeek.Sunday, 
                TimeTo = "20:49:59"
            };

            var result = testPeriod.IsBetween(DayOfWeek.Wednesday, "19:00:00");
            Assert.IsFalse(result);

            result = testPeriod.IsBetween(DayOfWeek.Saturday, "19:00:00");

            Assert.IsTrue(result);

            result = testPeriod.IsBetween(DayOfWeek.Sunday, "19:00:00");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void FromLessThanTo()
        {

            var testPeriod = new TestWeeklyPeriod
            {
                DayOfWeekFrom = DayOfWeek.Monday,
                TimeFrom = "20:55:00",
                DayOfWeekTo = DayOfWeek.Tuesday,
                TimeTo = "20:49:59"
            };

            var result = testPeriod.IsBetween(DayOfWeek.Monday, "21:00:00");

            Assert.IsTrue(result);

            result = testPeriod.IsBetween(DayOfWeek.Tuesday, "19:00:00");

            Assert.IsTrue(result);


            result = testPeriod.IsBetween(DayOfWeek.Sunday, "21:00:00");

            Assert.IsFalse(result);

            result = testPeriod.IsBetween(DayOfWeek.Wednesday, "21:00:00");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TestYearLastTwoDigits()
        {
            Assert.AreEqual(2017.YearLastTwoDigits(), "17");

            Assert.AreEqual(15.YearLastTwoDigits(), "15");

        }
    }
}
