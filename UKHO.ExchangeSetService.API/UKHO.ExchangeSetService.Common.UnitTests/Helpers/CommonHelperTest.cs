using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class CommonHelperTest
    {
        private ILogger<FileShareService> fakeLogger;
        public int retryCount = 3;
        private const double sleepDuration = 2; 
        const string TestClient = "TestClient";
        private bool _isRetryCalled;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<FileShareService>>();
        }

        #region SalesCatalogueResponse
        private SalesCatalogueResponse GetSalesCatalogueFileSizeResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 6,
                        RequestedProductsAlreadyUpToDateCount = 8,
                        ReturnedProductCount = 2,
                        RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                                new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                                new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                            }
                    },
                    Products = new List<Products> {
                            new Products {
                                ProductName = "productName",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> { 3, 4 },
                                Cancellation = new Cancellation {
                                    EditionNumber = 4,
                                    UpdateNumber = 6
                                },
                                FileSize = 500
                            }
                        }
                }
            };
        }
        #endregion SalesCatalogueResponse
        [Test]
        public void CheckMethodReturns_CorrectWeekNumer()
        {
            var week1 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/07"));
            var week26 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/07/01"));
            var week53 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/01"));

            Assert.AreEqual(1, week1);
            Assert.AreEqual(26, week26);
            Assert.AreEqual(53, week53);
        }
        [Test]
        public void CheckConversionOfBytesToMegabytes()
        {
            var fileSize = CommonHelper.ConvertBytesToMegabytes((long)4194304);
            Assert.AreEqual(4, fileSize);
        }

        [Test]
        public void CheckGetFileSize()
        {
            SalesCatalogueResponse salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
            Assert.AreEqual(500, fileSize);
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;
            retryCount = 1;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "File Share", Common.Logging.EventIds.RetryHttpClientFSSRequest, retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.False(_isRetryCalled);
            Assert.AreEqual(HttpStatusCode.TooManyRequests, result.StatusCode);
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            // Arrange 
            IServiceCollection services = new ServiceCollection();
            _isRetryCalled = false;

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", Common.Logging.EventIds.RetryHttpClientSCSRequest, retryCount, sleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            // Act
            var result = await configuredClient.GetAsync("https://test.com");

            // Assert
            Assert.False(_isRetryCalled);
            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, result.StatusCode);

        }
    }
}
