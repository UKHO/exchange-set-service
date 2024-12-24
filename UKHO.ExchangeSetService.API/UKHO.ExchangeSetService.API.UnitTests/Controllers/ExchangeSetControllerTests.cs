using System;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private IExchangeSetStandardService _fakeExchangeSetService;
        private ILogger<ExchangeSetController> _fakeLogger;
        private ExchangeSetController _controller;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeExchangeSetService = A.Fake<IExchangeSetStandardService>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetController>>();
            _controller = new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, _fakeExchangeSetService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetStandardService");
        }

        [Test]
        public async Task WhenProductNamesisPassed_ReturnsAcceptedResult()
        {
            string exchangeSetStandard = "S100";
            string[] productNames = { "Product1", "Product2" };
            string callbackUri = "http://callback.uri";
            var correlationId = "correlationId";

            A.CallTo(() => _fakeExchangeSetService.CreateProductDataByProductNames(A<string[]>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse()));
            A.CallTo(() => _fakeHttpContextAccessor.HttpContext.TraceIdentifier).Returns(correlationId);

            var response = await _controller.PostProductNames(exchangeSetStandard, productNames, callbackUri);

            response.Should().BeOfType<ObjectResult>();
            (response as ObjectResult).StatusCode.Should().Be((int)HttpStatusCode.Accepted);
        }
    }
}
