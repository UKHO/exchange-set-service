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

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    public class SalesCatalougeServiceTests
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private IOptions<SalesCatalogueConfiguration> fakeSaleCatalogueConfig;
        private IAuthTokenProvider fakeAuthTokenProvider;
        private ISalesCatalogueClient fakeSalesCatalogueClient;
        private ISalesCatalogueService salesCatalogueService;
        private HttpClient httpclient;

        [SetUp]
        public void Setup()
        {
            this.fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            this.fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();
            this.fakeSaleCatalogueConfig = A.Fake<IOptions<SalesCatalogueConfiguration>>();
            this.fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();

            salesCatalogueService = new SalesCatalogueService(fakeSalesCatalogueClient, fakeLogger, fakeAuthTokenProvider, fakeSaleCatalogueConfig);
        }

        #region ProductIdentifiers
        [Test]
        public async Task WhenSCSClientReturnsOtherThan200And304_ThenSCSClientCodeAndNullRsposnseReturns()
        {
            A.CallTo(() => fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, null, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request")))});
            var response = await salesCatalogueService.GetProductsFromSpecificDateAsync(DateTime.UtcNow.ToString());
            Assert.AreEqual(HttpStatusCode.BadRequest,response.ResponseCode, $"Expected {HttpStatusCode.BadRequest} got {response.ResponseCode}");
            Assert.IsNull(response.ResponseBody);
        }
        #endregion ProductIdentifiers
    }
}
