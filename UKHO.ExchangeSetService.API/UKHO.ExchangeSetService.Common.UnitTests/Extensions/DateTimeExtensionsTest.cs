using System;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Extensions;

namespace UKHO.ExchangeSetService.Common.UnitTests.Extensions
{
    public class DateTimeExtensionsTest
    {
        [Test]
        public void CheckIsValidDateWithValidDate_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("08Sep2021", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2021, 9, 8)));
            }
        }

        [Test]
        public void CheckIsValidDateWithInvalidLeapYear_ThenReturnFalse()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("29Feb2001", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.False);
                Assert.That(fakeFolderDateTime, Is.Default);
            }
        }

        [Test]
        public void CheckIsValidDateWithValidLeapYear_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("29Feb2000", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2000, 2, 29)));
            }
        }

        [Test]
        public void CheckIsValidDateWithValidDateAndFormat_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("20210908", "yyyyMMdd", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2021, 9, 8)));
            }
        }

        [Test]
        public void CheckIsValidDateWithInvalidLeapYearAndFormat_ThenReturnFalse()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("20010229", "yyyyMMdd", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.False);
                Assert.That(fakeFolderDateTime, Is.Default);
            }
        }

        [Test]
        public void CheckIsValidDateWithValidLeapYearAndFormat_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidDate("20000229", "yyyyMMdd", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2000, 2, 29)));
            }
        }

        [Test]
        public void CheckIsValidDateWithValidDateRfc1123Format_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidRfc1123Format("Sun, 21 Oct 2018 12:16:24 GMT", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2018, 10, 21, 12, 16, 24)));
            }
        }

        [Test]
        public void CheckIsValidDateWithInvalidRfc1123FormatLeapYear_ThenReturnFalse()
        {
            var isValidDate = DateTimeExtensions.IsValidRfc1123Format("Wed, 29 Feb 2021 12:16:24 GMT", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.False);
                Assert.That(fakeFolderDateTime, Is.Default);
            }
        }

        [Test]
        public void CheckIsValidDateWithValidRfc1123FormatLeapYear_ThenReturnTrue()
        {
            var isValidDate = DateTimeExtensions.IsValidRfc1123Format("Tue, 29 Feb 2000 12:16:24 GMT", out var fakeFolderDateTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(isValidDate, Is.True);
                Assert.That(fakeFolderDateTime, Is.EqualTo(new DateTime(2000, 2, 29, 12, 16, 24)));
            }
        }
    }
}
