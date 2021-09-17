using NUnit.Framework;
using System;
using UKHO.ExchangeSetService.API.Extensions;

namespace UKHO.ExchangeSetService.API.UnitTests.Extensions
{
    [TestFixture]
    public class DateTimeExtensionsTest
    {
        [Test]
        public void CheckIsValidDateWithValidDate_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("08Sep2021", out DateTime fakeFolderDateTime);
            Assert.IsTrue(isValidDate);
        }

        [Test]
        public void CheckIsValidDateWithInvalidLeapYear_ThenReturnFalse()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("29Feb2001", out DateTime fakeFolderDateTime);
            Assert.IsFalse(isValidDate);
        }

        [Test]
        public void CheckIsValidDateWithValidLeapYear_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("29Feb2000", out DateTime fakeFolderDateTime);
            Assert.IsTrue(isValidDate);
        }
    }
}