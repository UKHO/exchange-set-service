using NUnit.Framework;
using System;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class CommonHelperTest
    {
        [Test]
        public void CheckMethodReturns_CorrectWeekNumer()
        {
            var week1 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/07"));
            var week26 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/07/01"));
            var week53 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/01"));

            Assert.AreEqual(1, week1);
            Assert.AreEqual(26, week26);
            Assert.AreEqual(53, week53);
        }
    }
}
