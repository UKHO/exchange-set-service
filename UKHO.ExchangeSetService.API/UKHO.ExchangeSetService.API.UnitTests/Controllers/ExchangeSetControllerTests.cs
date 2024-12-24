using System;
using System.Threading.Tasks;
using System.Threading;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using System.Collections.Generic;
using System.Linq;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private IExchangeSetStandardService _fakeExchangeSetStandardService;
        private ILogger<ExchangeSetController> _fakeLogger;

        private ExchangeSetController _controller;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeExchangeSetStandardService = A.Fake<IExchangeSetStandardService>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetController>>();

            _controller = new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, _fakeExchangeSetStandardService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetStandardService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetStandardService");
        }

        #region ExchangeSetStandardProductVersions

        [Test]
        public async Task WhenExchangeSetStandardProductVersionsRequested_ThenPostProductVersions_Returns202Accepted()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new () { ProductName = "DE416080", EditionNumber = 9, UpdateNumber = 1 },
                new () { ProductName = "DE416081", EditionNumber = 10, UpdateNumber = 2 },
            };

            A.CallTo(() => _fakeExchangeSetStandardService.CreateExchangeSetByProductVersions(A<ProductVersionsRequest>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse())));

            var result = await _controller.PostProductVersions(productVersionRequest, "https://callback.uri", "s100");

            result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductVersionsRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductVersionsRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenInvalidExchangeSetStandardProductVersionsRequested_ThenPostProductVersions_ReturnsBadRequest()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new () { ProductName = "", EditionNumber = 9, UpdateNumber = 1 },
                new () { ProductName = "", EditionNumber = 10, UpdateNumber = 2 },
            };

            A.CallTo(() => _fakeExchangeSetStandardService.CreateExchangeSetByProductVersions(A<ProductVersionsRequest>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = Guid.NewGuid().ToString(), Errors = new List<Error> { new() { Source = "test", Description = "test error" } } })));

            var result = await _controller.PostProductVersions(productVersionRequest, "https://callback.uri", "s100");

            result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductVersionsRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductVersionsRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ProductVersions endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        #endregion ExchangeSetStandardProductVersions
    }
}
