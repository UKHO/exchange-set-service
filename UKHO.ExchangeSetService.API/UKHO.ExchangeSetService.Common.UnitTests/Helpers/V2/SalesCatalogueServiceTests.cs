// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
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
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using SalesCatalogueService = UKHO.ExchangeSetService.Common.Helpers.V2.SalesCatalogueService;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using System.Linq;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers.V2
{
    [TestFixture]
    public class SalesCatalogueServiceTests
    {
        private ILogger<SalesCatalogueService> _fakeLogger;
        private IAuthScsTokenProvider _fakeAuthScsTokenProvider;
        private ISalesCatalogueClient _fakeSalesCatalogueClient;
        private IOptions<SalesCatalogueConfiguration> _fakeSalesCatalogueConfig;
        private IUriHelper _fakeUriHelper;

        private const ApiVersion ApiVersion = Models.Enums.ApiVersion.V2;
        private readonly string _correlationId = Guid.NewGuid().ToString();
        private readonly string _exchangeSetStandard = Models.V2.Enums.ExchangeSetStandard.s100.ToString();
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly string _accessToken = "test-token";

        private SalesCatalogueService _salesCatalogueService;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            _fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            _fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            _fakeSalesCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { BaseUrl = "https://test.com", Version = "v2", ResourceId = "testResource" });
            _fakeUriHelper = A.Fake<IUriHelper>();

            _salesCatalogueService = new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriHelper);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new SalesCatalogueService(null, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriHelper);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullAuthScsTokenProvider = () => new SalesCatalogueService(_fakeLogger, null, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, _fakeUriHelper);
            nullAuthScsTokenProvider.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("authScsTokenProvider");

            Action nullSalesCatalogueClient = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, null, _fakeSalesCatalogueConfig, _fakeUriHelper);
            nullSalesCatalogueClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueClient");

            Action nullSalesCatalogueConfig = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, null, _fakeUriHelper);
            nullSalesCatalogueConfig.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueConfig");

            Action nullUriHelper = () => new SalesCatalogueService(_fakeLogger, _fakeAuthScsTokenProvider, _fakeSalesCatalogueClient, _fakeSalesCatalogueConfig, null);
            nullUriHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("uriHelper");
        }

        [Test]
        public async Task WhenPostProductVersionsAsyncResponseIsOk_ThenReturnsSuccess()
        {
            var productVersions = new List<ProductVersionRequest> {
                new() { ProductName = "101GB40079ABCDEFG", EditionNumber = 1, UpdateNumber = 1 },
                new() { ProductName = "102NO32904820801012", EditionNumber = 2, UpdateNumber = 2 },
                new() { ProductName = "111US00_ches_dcf8_20190703T00Z", EditionNumber = 4, UpdateNumber = 4 }
            };

            var uri = new Uri("https://test.com/v2/products/S100/productVersions");
            A.CallTo(() => _fakeUriHelper.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);
            
            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_accessToken);

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(GetSalesCatalogueServiceResponseForProductVersions())),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(httpResponse);

            var result = await _salesCatalogueService.PostProductVersionsAsync(ApiVersion, _exchangeSetStandard, productVersions, _correlationId, _cancellationToken);

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Value.Should().NotBeNull();
            result.Value.ResponseCode.Should().Be(HttpStatusCode.OK);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId} Elapsed {Elapsed}").MustHaveHappened();
        }

        [Test]
        public async Task WhenPostProductVersionsAsyncResponseIsNotModified_ThenReturnsNotModified()
        {
            var productVersions = new List<ProductVersionRequest> {
                new() { ProductName = "101GB40079ABCDEFG", EditionNumber = 1, UpdateNumber = 1 }
            };

            var uri = new Uri("https://test.com/v2/products/S100/productVersions");
            A.CallTo(() => _fakeUriHelper.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);

            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_accessToken);

            var httpResponse = new HttpResponseMessage(HttpStatusCode.NotModified)
            {
                Content = new StringContent("NotModified"),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(httpResponse);

            var result = await _salesCatalogueService.PostProductVersionsAsync(ApiVersion, _exchangeSetStandard, productVersions, _correlationId, _cancellationToken);

            result.StatusCode.Should().Be(HttpStatusCode.NotModified);
            result.Value.Should().NotBeNull();
            result.Value.ResponseCode.Should().Be(HttpStatusCode.NotModified);

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Content is already up to date, no new content available in sales catalogue service with uri:{RequestUri} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId} Elapsed {Elapsed}").MustHaveHappened();
        }

        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.NotFound)]
        [TestCase(HttpStatusCode.InternalServerError)]

        public async Task WhenPostProductVersionsAsyncResponseIsNotOkOrNotModified_ReturnsExpectedResult(HttpStatusCode httpStatusCode)
        {
            var productVersions = new List<ProductVersionRequest> {
                new() { ProductName = "101GB40079ABCDEFG", EditionNumber = 1, UpdateNumber = 1 },
                new() { ProductName = "102NO32904820801012", EditionNumber = 2, UpdateNumber = 2 },
                new() { ProductName = "111US00_ches_dcf8_20190703T00Z", EditionNumber = 4, UpdateNumber = 4 }
            };

            var uri = new Uri("https://test.com/v2/products/S100/productVersions");
            A.CallTo(() => _fakeUriHelper.CreateUri(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<object[]>.Ignored)).Returns(uri);

            A.CallTo(() => _fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(_accessToken);

            var httpResponse = new HttpResponseMessage(httpStatusCode)
            {
                Content = new StringContent(Convert.ToString(httpStatusCode)),
                RequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            };
            A.CallTo(() => _fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<CancellationToken>.Ignored)).Returns(httpResponse);

            var result = await _salesCatalogueService.PostProductVersionsAsync(ApiVersion, _exchangeSetStandard, productVersions, _correlationId, _cancellationToken);

            result.StatusCode.Should().Be(httpStatusCode);
            result.Value.Should().BeNull();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service with uri:{RequestUri} and responded with error:{Error} | statuscode:{StatusCode} | _X-Correlation-ID:{CorrelationId}").MustHaveHappened();

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SCSPostProductVersionsV2RequestCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SalesCatalogueService PostProductVersions endpoint request for _X-Correlation-ID:{correlationId} Elapsed {Elapsed}").MustHaveHappened();
        }

        private static SalesCatalogueResponse GetSalesCatalogueServiceResponseForProductVersions()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ScsRequestDateTime = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 1,
                        RequestedProductsAlreadyUpToDateCount = 0,
                        ReturnedProductCount = 1,
                        RequestedProductsNotReturned = []
                    },
                    Products = [
                               new Products {
                                ProductName = "101GB40079ABCDEFG",
                                EditionNumber = 1,
                                UpdateNumbers = [2],
                                Dates = [new Dates { IssueDate =DateTime.Today.AddDays(-50), UpdateNumber=2}],
                                FileSize = 900000000
                            },
                            new Products {
                        ProductName = "102NO32904820801012",
                        EditionNumber = 2,
                        UpdateNumbers = [3, 4],
                        Dates = [new Dates { IssueDate =DateTime.Today.AddDays(-50), UpdateNumber=3},
                                new Dates{IssueDate=DateTime.Today, UpdateNumber = 4}],
                        FileSize = 900000000
                    },
                    new Products {
                                ProductName = "111US00_ches_dcf8_20190703T00Z",
                                EditionNumber = 4,
                                UpdateNumbers = [4],
                                Dates = [new Dates { IssueDate =DateTime.Today.AddDays(-50), UpdateNumber=4}],
                                FileSize = 1000000
                            }
                           ]
                }
            };
        }
    }
}
