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

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private IHttpContextAccessor _fakeHttpContextAccessor;
        private IExchangeSetService _fakeExchangeSetService;
        private ILogger<ExchangeSetController> _fakeLogger;

        private ExchangeSetController _controller;

        [SetUp]
        public void Setup()
        {
            _fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            _fakeExchangeSetService = A.Fake<IExchangeSetService>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetController>>();

            _controller = new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, _fakeExchangeSetService);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetService");
        }

        #region ExchangeSetStandardProductVersions

        [Test]
        public async Task WhenExchangeSetStandardProductVersionsRequested_ThenPostProductVersions_Returns202Accepted()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new ProductVersionRequest { ProductName = "DE416080", EditionNumber = 9, UpdateNumber = 1 },
                new ProductVersionRequest { ProductName = "DE416081", EditionNumber = 10, UpdateNumber = 2 },
            };

            A.CallTo(() => _fakeExchangeSetService.CreateExchangeSetByProductVersions(A<ProductVersionsRequest>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.Accepted(new ExchangeSetResponse())));

            var result = await _controller.PostProductVersions(productVersionRequest, "https://callback.uri", "s100");

            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);
        }

        [Test]
        public async Task WhenInvalidExchangeSetStandardProductVersionsRequested_ThenPostProductVersions_ReturnsBadRequest()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new ProductVersionRequest { ProductName = "", EditionNumber = 9, UpdateNumber = 1 },
                new ProductVersionRequest { ProductName = "", EditionNumber = 10, UpdateNumber = 2 },
            };

            A.CallTo(() => _fakeExchangeSetService.CreateExchangeSetByProductVersions(A<ProductVersionsRequest>.Ignored, A<CancellationToken>.Ignored))
                .Returns(Task.FromResult(ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = Guid.NewGuid().ToString(), Errors = new List<Error> { new() { Source = "test", Description = "test error" } } })));

            var result = await _controller.PostProductVersions(productVersionRequest, "https://callback.uri", "s100");

            result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        #endregion ExchangeSetStandardProductVersions
    }
}
