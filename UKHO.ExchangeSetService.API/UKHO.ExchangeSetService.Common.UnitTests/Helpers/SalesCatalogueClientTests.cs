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
            var authToken = "test-token";
            var uri = "https://example.com/api";
            var correlationId = "test-correlation-id";
            var cancellationToken = CancellationToken.None;

            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
            A.CallTo(() => _httpClient.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored))
                .Returns(expectedResponse);

            var response = await _salesCatalogueClient.CallSalesCatalogueServiceApi(method, requestBody, authToken, uri, correlationId, cancellationToken);

            response.Should().Be(expectedResponse);
        }
    }
}
