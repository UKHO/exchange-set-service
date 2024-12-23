using NUnit.Framework;
using System;
using UKHO.ExchangeSetService.Common.Extensions;

namespace UKHO.ExchangeSetService.Common.UnitTests.Extensions
{
    public class DateTimeExtensionsTest
    {
        [Test]
        public void CheckIsValidDateWithValidDate_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("08Sep2021", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.True);
        }

        [Test]
        public void CheckIsValidDateWithInvalidLeapYear_ThenReturnFalse()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("29Feb2001", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.False);
        }

        [Test]
        public void CheckIsValidDateWithValidLeapYear_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("29Feb2000", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.True);
        }

        [Test]
        public void CheckIsValidDateWithValidDateRfc1123Format_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidRfc1123Format("Sun, 21 Oct 2018 12:16:24 GMT", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.True);
        }

        [Test]
        public void CheckIsValidDateWithInvalidRfc1123FormatLeapYear_ThenReturnFalse()
        {
            bool isValidDate = DateTimeExtensions.IsValidRfc1123Format("Wed, 29 Feb 2021 12:16:24 GMT", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.False);
        }

        [Test]
        public void CheckIsValidDateWithValidRfc1123FormatLeapYear_ThenReturnTrue()
        {
            bool isValidDate = DateTimeExtensions.IsValidRfc1123Format("Tue, 29 Feb 2000 12:16:24 GMT", out DateTime fakeFolderDateTime);
            Assert.That(isValidDate, Is.True);
        }
    }
}
