
using System;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();

        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }
    }
}
