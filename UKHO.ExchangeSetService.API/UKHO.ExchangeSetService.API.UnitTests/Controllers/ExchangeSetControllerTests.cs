using System;
using System.Collections.Generic;
using System.Threading;
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
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private ILogger<ExchangeSetController> _fakeLogger;
        private IExchangeSetStandardService _fakeExchangeSetStandardService;

        private ExchangeSetController _controller;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetController>>();
            _fakeExchangeSetStandardService = A.Fake<IExchangeSetStandardService>();

            _controller = new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, _fakeExchangeSetStandardService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetStandardService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetService");
        }

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenPostUpdatesSince_Returns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = "Tue, 10 Dec 2024 05:46:00 GMT" };

            A.CallTo(() => _fakeExchangeSetStandardService.CreateUpdatesSince(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse())));

            var result = await _controller.PostUpdatesSince("s100", updatesSinceRequest, "s101", "http://callback.uri");

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        }

        [Test]
        public async Task WhenInValidSinceDateTimeRequested_ThenPostUpdatesSince_ReturnsBadRequest()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = "" };

            A.CallTo(() => _fakeExchangeSetStandardService.CreateUpdatesSince(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = Guid.NewGuid().ToString(), Errors = new List<Error> { new() { Source = "test", Description = "test error" } } })));

            var result = await _controller.PostUpdatesSince("s100", updatesSinceRequest, "s101", "http://callback.uri");

            result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }
    }
}
