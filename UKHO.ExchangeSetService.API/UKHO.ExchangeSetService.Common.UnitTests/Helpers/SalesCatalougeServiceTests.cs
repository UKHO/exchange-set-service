using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using System.Collections.Generic;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Models.Request;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class SalesCatalougeServiceTests
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private IOptions<SalesCatalogueConfiguration> fakeSaleCatalogueConfig;
        private IAuthScsTokenProvider fakeAuthScsTokenProvider;
        private ISalesCatalogueClient fakeSalesCatalogueClient;
        private ISalesCatalogueService salesCatalogueService;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            this.fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            this.fakeSaleCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { ProductType = "Test", Version = "t1", CatalogueType = "essTest" });
            this.fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            salesCatalogueService = new SalesCatalogueService(fakeSalesCatalogueClient, fakeLogger, fakeAuthScsTokenProvider, fakeSaleCatalogueConfig);
        }

        private static SalesCatalogueProductResponse GetSalesCatalogueServiceResponse()
        {
            return new SalesCatalogueProductResponse()
            {
                ProductCounts = new ProductCounts()
                {
                    RequestedProductCount = 12,
                    RequestedProductsAlreadyUpToDateCount = 5,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>
                    {
                        new RequestedProductsNotReturned()
                        {
                            ProductName = "test",
                            Reason = "notfound"
                        }
                    },
                    ReturnedProductCount = 4
                }
            };
        }

        #region GetSalesCatalogueDataProductResponse
        private List<SalesCatalogueDataProductResponse> GetSalesCatalogueDataProductResponse()
        {
            return
                new List<SalesCatalogueDataProductResponse>()
                {
                    new SalesCatalogueDataProductResponse()
                    {
                    ProductName = "10000002",
                    LatestUpdateNumber = 5,
                    FileSize = 600,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 119,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitEasternmostLatitude = 120,
                    BaseCellEditionNumber = 3,
                    BaseCellLocation = "M0;B0",
                    BaseCellIssueDate = DateTime.Today,
                    BaseCellUpdateNumber = 0,
                    Encryption = true,
                    CancelledCellReplacements = new List<string>() { },
                    Compression = true,
                    IssueDateLatestUpdate = DateTime.Today,
                    LastUpdateNumberPreviousEdition = 0,
                    TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                    }
                };
        }
        #endregion

        #region GetProductsFromSpecificDateAsync
        [Test]
        public async Task WhenSCSClientReturnsOtherThan200And304_ThenGetProductsFromSpecificDateAsyncReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });
            var response = await salesCatalogueService.GetProductsFromSpecificDateAsync(DateTime.UtcNow.ToString(), string.Empty);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.That(response.ResponseBody,Is.Null);
        }

        [Test]
        public async Task WhenSCSClientReturns304_ThenGetProductsFromSpecificDateAsyncReturns304AndLastModifiedDateInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotModified, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Ignore"))), Headers = { Date = DateTime.UtcNow } };
            DateTimeOffset lastModified = DateTime.UtcNow;
            httpResponse.Content.Headers.LastModified = lastModified;
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.GetProductsFromSpecificDateAsync(DateTime.UtcNow.ToString(), string.Empty);
            Assert.AreEqual(HttpStatusCode.NotModified, response.ResponseCode, $"Expected {HttpStatusCode.NotModified} got {response.ResponseCode}");
            Assert.That(lastModified.UtcDateTime, Is.EqualTo(response.LastModified));
        }

        [Test]
        public async Task WhenSCSClientReturns200_ThenGetProductsFromSpecificDateAsyncReturns200AndDataInResponse()
        {
            SalesCatalogueProductResponse scsResponse = GetSalesCatalogueServiceResponse();

            var jsonString = JsonConvert.SerializeObject(scsResponse);
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.GetProductsFromSpecificDateAsync(DateTime.UtcNow.ToString(), string.Empty);
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode, $"Expected {HttpStatusCode.OK} got {response.ResponseCode}");
            Assert.AreEqual(jsonString, JsonConvert.SerializeObject(response.ResponseBody));
        }

        [Test]
        public async Task WhenGetProductsFromSpecificDateAsyncCallsApi_ThenValidateCorrectParametersArePassed()
        {
            //Data
            string actualAccessToken = "notRequiredDuringTesting";
            string postBodyParam = "This should be null when passed to api call";
            string sinceDateTime = DateTime.UtcNow.ToString();

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var scsResponse = new SalesCatalogueProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);
            string correlationIdParam = null;

            //Mock
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await salesCatalogueService.GetProductsFromSpecificDateAsync(sinceDateTime, string.Empty);

            //Test
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.AreEqual(HttpMethod.Get, httpMethodParam);
            Assert.AreEqual($"/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/products?sinceDateTime={sinceDateTime}", uriParam);
            Assert.IsNull(postBodyParam);
            Assert.AreEqual(actualAccessToken, accessTokenParam);
        }
        #endregion GetProductsFromSpecificDateAsync

        #region PostProductVersionsAsync
        [Test]
        public async Task WhenSCSClientReturnsOtherThan200And304_ThenPostProductVersionsAsyncReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });
            var response = await salesCatalogueService.PostProductVersionsAsync(new List<ProductVersionRequest> { new ProductVersionRequest() { EditionNumber = 1, ProductName = "TEST1", UpdateNumber = 0 } }, string.Empty);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }

        [Test]
        public async Task WhenSCSClientReturns304_ThenPostProductVersionsAsyncReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotModified, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Ignore"))), Headers = { Date = DateTime.UtcNow } };
            DateTimeOffset lastModified = DateTime.UtcNow;
            httpResponse.Content.Headers.LastModified = lastModified;
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.PostProductVersionsAsync(new List<ProductVersionRequest> { new ProductVersionRequest() { EditionNumber = 1, ProductName = "TEST1", UpdateNumber = 0 } }, String.Empty);
            Assert.AreEqual(HttpStatusCode.NotModified, response.ResponseCode, $"Expected {HttpStatusCode.NotModified} got {response.ResponseCode}");
            Assert.AreEqual(lastModified.UtcDateTime, response.LastModified);
        }

        [Test]
        public async Task WhenSCSClientReturns200_ThenPostProductVersionsAsyncReturns200AndDataInResponse()
        {
            SalesCatalogueProductResponse scsResponse = GetSalesCatalogueServiceResponse();

            var jsonString = JsonConvert.SerializeObject(scsResponse);
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.PostProductVersionsAsync(new List<ProductVersionRequest> { new ProductVersionRequest() { EditionNumber = 1, ProductName = "TEST1", UpdateNumber = 0 } }, String.Empty);
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode, $"Expected {HttpStatusCode.OK} got {response.ResponseCode}");
            Assert.AreEqual(jsonString, JsonConvert.SerializeObject(response.ResponseBody));
        }

        [Test]
        public async Task WhenPostProductVersionsAsyncCallsApi_ThenValidateCorrectParametersArePassed()
        {
            //Data
            string actualAccessToken = "notRequiredDuringTesting";
            var requestBody = new List<ProductVersionRequest> { new ProductVersionRequest() { EditionNumber = 1, ProductName = "TEST1", UpdateNumber = 0 } };
            string postBodyParam = "This should be replaced by actual value";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var scsResponse = new SalesCatalogueProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);
            string correlationIdParam = null;

            //Mock
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await salesCatalogueService.PostProductVersionsAsync(requestBody, string.Empty);

            //Test
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.AreEqual(HttpMethod.Post, httpMethodParam);
            Assert.AreEqual(uriParam, $"/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/products/productVersions");
            Assert.AreEqual(JsonConvert.SerializeObject(requestBody), postBodyParam);
            Assert.AreEqual(accessTokenParam, actualAccessToken);
        }
        #endregion PostProductVersionsAsync

        #region PostProductIdentifiersAsync
        [Test]
        public async Task WhenSCSClientReturnsOtherThan200And304_ThenPostProductIdentifiersAsyncReturnsSameStatusAndNullInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });
            var response = await salesCatalogueService.PostProductIdentifiersAsync(new List<string> { "TEST1", "TEST2" }, string.Empty);
            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }

        [Test]
        public async Task WhenSCSClientReturns304_ThenPostProductIdentifiersAsync304AndLastModifiedDateInResponse()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.NotModified, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Ignore"))), Headers = { Date = DateTime.UtcNow } };
            DateTimeOffset lastModified = DateTime.UtcNow;
            httpResponse.Content.Headers.LastModified = lastModified;
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.PostProductIdentifiersAsync(new List<string> { "TEST1", "TEST2" }, string.Empty);
            Assert.AreEqual(HttpStatusCode.NotModified, response.ResponseCode, $"Expected {HttpStatusCode.NotModified} got {response.ResponseCode}");
            Assert.AreEqual(lastModified.UtcDateTime, response.LastModified);
        }

        [Test]
        public async Task WhenSCSClientReturns200_ThenPostProductIdentifiersAsyncReturns200AndDataInResponse()
        {
            SalesCatalogueProductResponse scsResponse = GetSalesCatalogueServiceResponse();

            var jsonString = JsonConvert.SerializeObject(scsResponse);
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);
            var response = await salesCatalogueService.PostProductIdentifiersAsync(new List<string> { "TEST1", "TEST2" }, string.Empty);
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode, $"Expected {HttpStatusCode.OK} got {response.ResponseCode}");
            Assert.AreEqual(jsonString, JsonConvert.SerializeObject(response.ResponseBody));
        }

        [Test]
        public async Task WhenPostProductIdentifiersAsyncCallsApi_ThenValidateCorrectParametersArePassed()
        {
            //Data
            var requestBody = new List<string> { "TEST1", "TEST2" };
            string actualAccessToken = "notRequiredDuringTesting";
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var scsResponse = new SalesCatalogueProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);
            string correlationIdParam = null;

            //Mock
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))), Headers = { Date = DateTime.UtcNow } };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await salesCatalogueService.PostProductIdentifiersAsync(requestBody, string.Empty);

            //Test
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.AreEqual(HttpMethod.Post, httpMethodParam);
            Assert.AreEqual($"/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/products/productIdentifiers", uriParam);
            Assert.AreEqual(JsonConvert.SerializeObject(requestBody), postBodyParam);
            Assert.AreEqual(accessTokenParam, actualAccessToken);
        }
        #endregion PostProductIdentifiersAsync

        #region GetSalesCatalogueDataResponse
        [Test]
        public void WhenSCSClientReturnsOtherThan200_ThenGetSalesCatalogueDataResponseReturnsFulfilmentException()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await salesCatalogueService.GetSalesCatalogueDataResponse(fakeBatchId, null); });
        }

        [Test]
        public async Task WhenSCSClientReturns200_ThenGetSalesCatalogueDataResponseReturns200AndDataInResponse()
        {
            List<SalesCatalogueDataProductResponse> scsResponse = GetSalesCatalogueDataProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await salesCatalogueService.GetSalesCatalogueDataResponse(fakeBatchId, null);

            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode, $"Expected {HttpStatusCode.OK} got {response.ResponseCode}");
            Assert.AreEqual(jsonString, JsonConvert.SerializeObject(response.ResponseBody));
        }

        [Test]
        public async Task WhenGetSalesCatalogueDataResponseCallsApi_ThenValidateCorrectParametersArePassed()
        {
            //Data
            string actualAccessToken = "notRequiredDuringTesting";
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            //Test variable
            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var scsResponse = new List<SalesCatalogueDataResponse>();
            var jsonString = JsonConvert.SerializeObject(scsResponse);
            string correlationIdParam = null;

            //Mock
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri, string correlationId) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                    correlationIdParam = correlationId;
                })
                .Returns(httpResponse);

            //Method call
            var response = await salesCatalogueService.GetSalesCatalogueDataResponse(fakeBatchId, null);

            //Test
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.AreEqual(HttpMethod.Get, httpMethodParam);
            Assert.AreEqual($"/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/catalogue/{fakeSaleCatalogueConfig.Value.CatalogueType}", uriParam);
            Assert.AreEqual(actualAccessToken, accessTokenParam);
        }
        #endregion GetSalesCatalogueDataResponse
    }
}
