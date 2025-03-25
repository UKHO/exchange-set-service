using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.UnitTests.HealthCheck
{
    public class SalesCatalogueServiceHealthCheckTest
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private IOptions<SalesCatalogueConfiguration> fakeSaleCatalogueConfig;
        private IAuthScsTokenProvider fakeAuthScsTokenProvider;
        private ISalesCatalogueClient fakeSalesCatalogueClient;
        private SalesCatalogueServiceHealthCheck salesCatalogueServiceHealthCheck;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            this.fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            this.fakeSaleCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { ProductType = "Test", Version = "t1", CatalogueType = "essTest" });
            this.fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();

            salesCatalogueServiceHealthCheck = new SalesCatalogueServiceHealthCheck(fakeSalesCatalogueClient, fakeAuthScsTokenProvider, fakeSaleCatalogueConfig, fakeLogger);
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

        [Test]
        public async Task WhenSCSClientReturnsOtherThan200And304_ThenSalesCatalogueServiceIsHealthy()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("BadRequest"))) });

            var response = await salesCatalogueServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }

        [Test]
        public async Task WhenSCSClientReturns200_ThenSalesCatalogueServiceIsHealthy()
        {
            List<SalesCatalogueDataProductResponse> scsResponse = GetSalesCatalogueDataProductResponse();

            var jsonString = JsonConvert.SerializeObject(scsResponse);
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await salesCatalogueServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task WhenSCSClientReturns503_ThenSalesCatalogueServiceIsUnhealthy()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.ServiceUnavailable, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("ServiceUnavailable"))) });

            var response = await salesCatalogueServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }

        [Test]
        public async Task WhenSCSClientThrowsException_ThenSalesCatalogueServiceIsUnhealthy()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Throws<Exception>();

            var response = await salesCatalogueServiceHealthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(response.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }
    }
}
