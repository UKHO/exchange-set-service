// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class SalesCatalogueClientTests
    {
        private readonly string _correlationId = Guid.NewGuid().ToString();
        private readonly CancellationToken _cancellationToken = CancellationToken.None;
        private readonly string _fakeAuthToken = "fake-token";
        private readonly string _baseUrl = "https://example.com";

        private IHttpClientFactory _httpClientFactory;
        private HttpClient _httpClient;
        private SalesCatalogueClient _salesCatalogueClient;

        [SetUp]
        public void SetUp()
        {
            _httpClientFactory = A.Fake<IHttpClientFactory>();
            _httpClient = A.Fake<HttpClient>();
            A.CallTo(() => _httpClientFactory.CreateClient(A<string>.Ignored)).Returns(_httpClient);

            _salesCatalogueClient = new SalesCatalogueClient(_httpClientFactory);
        }

        [Test]
        public async Task WhenValidDataIsPassed_ThenCallSalesCatalogueServiceApiReturnsOkHttpResponseMessage()
        {
            var method = HttpMethod.Get;
            var requestBody = "{\"key\":\"value\"}";

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored))
                .Returns(expectedResponse);

            var response = await _salesCatalogueClient.CallSalesCatalogueServiceApi(method, requestBody, _fakeAuthToken, _baseUrl, _correlationId, _cancellationToken);

            response.Should().Be(expectedResponse);
        }

        [Test]
        public async Task WhenInvalidUriIsPassed_ThenCallSalesCatalogueServiceApiThrowsArgumentException()
        {
            var method = HttpMethod.Get;
            var requestBody = "{\"key\":\"value\"}";
            var uri = "invalid-uri";
            await FluentActions.Invoking(async () => await _salesCatalogueClient.CallSalesCatalogueServiceApi(method, requestBody, _fakeAuthToken, uri, _correlationId, _cancellationToken))
                .Should()
                .ThrowExactlyAsync<ArgumentException>().WithMessage("The provided URI is not valid. (Parameter 'uri')");
        }

        [Test]
        public async Task WhenNoCorrelationIdIsPassed_ThenCallSalesCatalogueServiceApiDoesNotAddCorrelationIdHeader()
        {
            var method = HttpMethod.Get;
            var requestBody = "{\"key\":\"value\"}";

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored))
                .Returns(expectedResponse);

            var response = await _salesCatalogueClient.CallSalesCatalogueServiceApi(method, requestBody, _fakeAuthToken, _baseUrl, cancellationToken: _cancellationToken);

            response.Should().Be(expectedResponse);
            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>.That.Matches(req => !req.Headers.Contains(SalesCatalogueClient.XCorrelationIdHeaderKey)), A<CancellationToken>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }
}
