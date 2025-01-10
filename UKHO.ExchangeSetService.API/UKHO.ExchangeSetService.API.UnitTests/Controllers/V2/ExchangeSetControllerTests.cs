// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers.V2;
using UKHO.ExchangeSetService.API.Services.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Enums;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using ExchangeSetStandard = UKHO.ExchangeSetService.Common.Models.V2.Enums.ExchangeSetStandard;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers.V2
{
    [TestFixture]
    public class ExchangeSetControllerTests
    {
        private const string CallbackUri = "https://callback.uri";
        private const string Iso8601DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

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
            Action nullHttpContextAccessor = () => new ExchangeSetController(null, _fakeLogger, _fakeExchangeSetStandardService);
            nullHttpContextAccessor.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("httpContextAccessor");

            Action nullLogger = () => new ExchangeSetController(_fakeHttpContextAccessor, null, _fakeExchangeSetStandardService);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullExchangeSetService = () => new ExchangeSetController(_fakeHttpContextAccessor, _fakeLogger, null);
            nullExchangeSetService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("exchangeSetStandardService");
        }

        [Test]
        public async Task WhenProductNamesIsPassed_ReturnsAcceptedResult()
        {
            string[] productNames = { "Product1", "Product2" };
            var correlationId = "correlationId";

            var exchangeSetServiceResponse = new ExchangeSetStandardServiceResponse()
            {
                ExchangeSetStandardResponse = GetExchangeSetResponse()
            };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessProductNamesRequestAsync(A<string[]>.Ignored, A<ApiVersion>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, CancellationToken.None))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>
                .Accepted(exchangeSetServiceResponse));

            A.CallTo(() => _fakeHttpContextAccessor.HttpContext.TraceIdentifier).Returns(correlationId);

            var response = await _controller.PostProductNames(ExchangeSetStandard.s100.ToString(), productNames, CallbackUri);
            response.Should().BeOfType<AcceptedResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductNamesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Names Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostProductNamesRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Names Endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenValidProductVersionsIsPassed_ThenPostProductVersionsReturns202Accepted()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new () { ProductName = "101GB40079ABCDEFG", EditionNumber = 7, UpdateNumber = 10 },
                new () { ProductName = "102NO32904820801012", EditionNumber = 36, UpdateNumber = 0 },
            };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessProductVersionsRequestAsync(A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
             .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>
             .Accepted(null));

            var result = await _controller.PostProductVersions(ExchangeSetStandard.s100.ToString(), productVersionRequest, CallbackUri);

            result.Should().BeOfType<AcceptedResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

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
        public async Task WhenInvalidProductVersionsIsPassed_ThenPostProductVersionsReturnsBadRequest()
        {
            var productVersionRequest = new List<ProductVersionRequest>
            {
                new () { ProductName = "", EditionNumber = 9, UpdateNumber = 1 },
                new () { ProductName = "", EditionNumber = 10, UpdateNumber = 2 },
            };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessProductVersionsRequestAsync(A<IEnumerable<ProductVersionRequest>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = Guid.NewGuid().ToString(), Errors = [new() { Source = "requestBody", Description = "Either body is null or malformed." }] }));

            var result = await _controller.PostProductVersions(ExchangeSetStandard.s100.ToString(), productVersionRequest, CallbackUri);

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

        [Test]
        public async Task WhenValidSinceDateTimeRequested_ThenPostUpdatesSinceReturns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture) };

            var exchangeSetServiceResponse = new ExchangeSetStandardServiceResponse()
            {
                ExchangeSetStandardResponse = GetExchangeSetResponse()
            };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessUpdatesSinceRequestAsync(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetServiceResponse));

            var result = await _controller.PostUpdatesSince(ExchangeSetStandard.s100.ToString(), updatesSinceRequest, "s100", CallbackUri);

            result.Should().BeOfType<AcceptedResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenValidSinceDateTimeAndEmptyProductIdentifierRequested_ThenPostUpdatesSinceReturns202Accepted()
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString(Iso8601DateTimeFormat, CultureInfo.InvariantCulture) };

            var exchangeSetServiceResponse = new ExchangeSetStandardServiceResponse()
            {
                ExchangeSetStandardResponse = GetExchangeSetResponse()
            };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessUpdatesSinceRequestAsync(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetServiceResponse));

            var result = await _controller.PostUpdatesSince(ExchangeSetStandard.s100.ToString(), updatesSinceRequest, "", CallbackUri);

            result.Should().BeOfType<AcceptedResult>().Which.StatusCode.Should().Be(StatusCodes.Status202Accepted);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenNullOrEmptySinceDateTimeRequested_ThenPostUpdatesSinceReturnsBadRequest()
        {
            A.CallTo(() => _fakeExchangeSetStandardService.ProcessUpdatesSinceRequestAsync(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Errors = [new() { Source = "requestBody", Description = "Either body is null or malformed." }]
                }));

            var result = await _controller.PostUpdatesSince(ExchangeSetStandard.s100.ToString(), null, "s100", CallbackUri);

            result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        [TestCase("101", "http://callback.uri")]
        [TestCase("101s", "http//callback.uri")]
        [TestCase("S101", "http:callback.uri")]
        public async Task WhenInValidDataRequested_ThenPostUpdatesSinceReturnsBadRequest(string inValidProductIdentifier, string inValidCallBackUri)
        {
            var updatesSinceRequest = new UpdatesSinceRequest { SinceDateTime = DateTime.UtcNow.AddDays(-10).ToString() };

            A.CallTo(() => _fakeExchangeSetStandardService.ProcessUpdatesSinceRequestAsync(A<UpdatesSinceRequest>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Errors = [new() { Source = "SinceDateTime", Description = "Provided sinceDateTime is either invalid or invalid format, the valid format is 'ISO 8601 format' (e.g. '2024-12-20T11:51:00.000Z')." },
                                  new() { Source = "ProductIdentifier", Description = "ProductIdentifier must be valid value" },
                                  new() { Source = "CallbackUri", Description = "Invalid callbackUri format." }]
                }));

            var result = await _controller.PostUpdatesSince(ExchangeSetStandard.s100.ToString(), updatesSinceRequest, inValidProductIdentifier, inValidCallBackUri);

            result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.PostUpdatesSinceRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "UpdatesSince endpoint request for _X-Correlation-ID:{correlationId} and ExchangeSetStandard:{exchangeSetStandard} Elapsed {Elapsed}").MustHaveHappened();
        }

        private ExchangeSetStandardResponse GetExchangeSetResponse()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/status"
            };
            LinkSetBatchDetailsUri linkSetBatchDetailsUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Links links = new()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetBatchDetailsUri = linkSetBatchDetailsUri,
                ExchangeSetFileUri = linkSetFileUri
            };

            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet =
            [
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "101GB40079ABCDEFG",
                    Reason = "invalidProduct"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "102NO32904820801012",
                    Reason = "invalidProduct"
                }
            ];

            ExchangeSetStandardResponse exchangeSetStandardResponse = new()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                ExchangeSetProductCount = 15,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet
            };
            return exchangeSetStandardResponse;
        }
    }
}
