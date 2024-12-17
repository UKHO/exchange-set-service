using System;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private IExchangeSetService _fakeExchangeSetService;
        private ILogger<ExchangeSetController> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeExchangeSetService = A.Fake<IExchangeSetService>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetController>>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetService");
        }
    }
}
