using NUnit.Framework;
using System;
using UKHO.ExchangeSetService.API.Extensions;

namespace UKHO.ExchangeSetService.API.UnitTests.Extensions
{
    [TestFixture]
    public class DateTimeExtensionsTest
    {
        [Test]
        public void CheckIsValidDate()
        {
            bool isValidDate = DateTimeExtensions.IsValidDate("08Sep2021", out DateTime fakeFolderDateTime);
            Assert.False(isValidDate);
        }
    }
}