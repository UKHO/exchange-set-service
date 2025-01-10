// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using SalesCatalogueService = UKHO.ExchangeSetService.Common.Helpers.V2.SalesCatalogueService;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers.V2
{
    [TestFixture]
    public class SalesCatalogueServiceTests
    {
        private ILogger<SalesCatalogueService> _fakeLogger;
        private IAuthScsTokenProvider _fakeAuthScsTokenProvider;
        private ISalesCatalogueClient _fakeSalesCatalogueClient;
        private IOptions<SalesCatalogueConfiguration> _fakeSalesCatalogueConfig;
        private IUriFactory _fakeUriFactory;

        private const ApiVersion ApiVersion = Models.Enums.ApiVersion.V2;
        private readonly string _correlationId = Guid.NewGuid().ToString();
        private readonly string _exchangeSetStandard = Models.V2.Enums.ExchangeSetStandard.s100.ToString();
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly string _fakeAuthToken = "fake-token";

        private SalesCatalogueService _salesCatalogueService;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            _fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            _fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _fakeSalesCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { BaseUrl = "https://test.com", Version = "v2", ResourceId = "testResource" });
            _fakeUriFactory = A.Fake<IUriFactory>();

            _salesCatalogueService = new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriFactory);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new SalesCatalogueService(null, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriFactory);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullAuthScsTokenProvider = () => new SalesCatalogueService(_fakeLogger, null, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriFactory);
            nullAuthScsTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("authScsTokenProvider");

            Action nullSalesCatalogueClient = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, null, _fakeSalesCatalogueConfig, _fakeUriFactory);
            nullSalesCatalogueClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueClient");

            Action nullSalesCatalogueConfig = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, null, _fakeUriFactory);
            nullSalesCatalogueConfig.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueConfig");

            Action nullUriHelper = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, null);
            nullUriHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("uriHelper");
        }

        #region PostProductNamesAsync

        [Test]
        public async Task WhenPostProductNamesAsyncIsCalled_ThenDependenciesAreCalled()
        {
            var productNames = new List<string> { "101GB40079ABCDEFG", "102NO32904820801012" };
            var uri = new Uri("https://test.com");

            A.CallTo(() => _fakeUriFactory.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_fakeAuthToken);
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
                .Returns(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(new SalesCatalogueProductResponse())) });

            await _salesCatalogueService.PostProductNamesAsync(ApiVersion, _exchangeSetStandard, productNames, _correlationId, _cancellationToken);

            A.CallTo(() => _fakeUriFactory.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPostProductNamesAsyncAsyncReturnsOk_ThenServiceResponseResultIsSuccess()
        {
            var productNames = new List<string> { "101GB40079ABCDEFG", "102NO32904820801012" };
            var uri = new Uri("https://test.com");

            A.CallTo(() => _fakeUriFactory.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_fakeAuthToken);

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(GetSalesCatalogueServiceResponse())),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored))
            .Returns(httpResponseMessage);

            var result = await _salesCatalogueService.PostProductNamesAsync(ApiVersion, _exchangeSetStandard, productNames, _correlationId, _cancellationToken);
            result.Value.Should().NotBeNull();
            result.Value.ResponseCode.Should().Be(HttpStatusCode.OK);
            result.Value.ScsRequestDateTime.ToString(CultureInfo.InvariantCulture).Should().NotBeNull();
            result.Value.ResponseBody.Should().BeEquivalentTo(GetSalesCatalogueServiceResponse());

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenPostProductNamesAsyncReturnsNotModified_ThenServiceResponseResultIsNotModified()
        {
            var productNames = new List<string> { "101GB40079ABCDEFG", "102NO32904820801012" };
            var uri = new Uri("https://test.com");

            A.CallTo(() => _fakeUriFactory.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_fakeAuthToken);

            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.NotModified)
            {
                Content = new StringContent("NotModified"),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };

            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(httpResponseMessage);

            var result = await _salesCatalogueService.PostProductNamesAsync(ApiVersion, _exchangeSetStandard, productNames, _correlationId, _cancellationToken);

            result.Should().NotBeNull();
            result.StatusCode.Should().Be(HttpStatusCode.NotModified);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId} Elapsed {Elapsed}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Content is already up to date, no new content available in sales catalogue service with uri:{RequestUri} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.InternalServerError)]
        public async Task WhenPostProductNamesAsyncReturnsOtherStatusCodes_ThenServiceResponseResultIsAsExpected(HttpStatusCode httpStatusCode)
        {
            var productNames = new List<string> { "101GB40079ABCDEFG", "102NO32904820801012" };
            var uri = new Uri("https://test.com");

            A.CallTo(() => _fakeUriFactory.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_fakeAuthToken);

            var httpResponseMessage = new HttpResponseMessage(httpStatusCode)
            {
                Content = new StringContent(Convert.ToString(httpStatusCode)!),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(httpResponseMessage);

            var result = await _salesCatalogueService.PostProductNamesAsync(ApiVersion, _exchangeSetStandard, productNames, _correlationId, _cancellationToken);

            result.StatusCode.Should().Be(httpStatusCode);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductNamesRequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post sales catalogue service for ProductNames for _X-Correlation-ID:{CorrelationId} Elapsed {Elapsed}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}").MustHaveHappenedOnceExactly();
        }

        #endregion PostProductNamesAsync

        private static SalesCatalogueProductResponse GetSalesCatalogueServiceResponse()
        {
            return new SalesCatalogueProductResponse
            {
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 2,
                    ReturnedProductCount = 1,
                    RequestedProductsAlreadyUpToDateCount = 0,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>
                    {
                        new()
                        {
                            ProductName = "102NO32904820801012",
                            Reason = "invalidProduct"
                        }
                    }
                },
                Products = new List<Products>
                {
                    new()
                    {
                        ProductName = "101GB40079ABCDEFG",
                        EditionNumber = 7,
                        UpdateNumbers = new List<int?> { 0 }
                    }
                }
            };
        }

    }
}
