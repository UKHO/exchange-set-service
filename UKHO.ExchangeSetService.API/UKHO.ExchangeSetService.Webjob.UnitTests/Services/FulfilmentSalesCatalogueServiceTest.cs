using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentSalesCatalogueServiceTest
    {
        private ISalesCatalogueService _fakeSalesCatalogueService;
        private FulfilmentSalesCatalogueService _fulfilmentSalesCatalogueService;
        private const string FakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";

        [SetUp]
        public void Setup()
        {
            _fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();

            _fulfilmentSalesCatalogueService = new FulfilmentSalesCatalogueService(_fakeSalesCatalogueService);
        }

        #region GetSalesCatalogueDataResponse
        private static SalesCatalogueDataResponse GetSalesCatalogueDataResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody =
                [
                    new SalesCatalogueDataProductResponse
                    {
                        ProductName="10000002",
                        LatestUpdateNumber=5,
                        FileSize=600,
                        CellLimitSouthernmostLatitude=24,
                        CellLimitWesternmostLatitude=119,
                        CellLimitNorthernmostLatitude=25,
                        CellLimitEasternmostLatitude=120
                    }
                ]
            };
        }
        #endregion

        [Test]
        public async Task WhenRequestGetSalesCatalogueDataResponse_ThenReturnsDataInResponse()
        {
            A.CallTo(() => _fakeSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueDataResponse());

            var response = await _fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchId, null);

            Assert.That(response, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(response.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response, Is.InstanceOf(typeof(SalesCatalogueDataResponse)));
            });
        }
    }
}
