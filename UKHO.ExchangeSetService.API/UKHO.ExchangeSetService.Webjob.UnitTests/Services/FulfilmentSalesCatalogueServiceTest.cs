using FakeItEasy;
using NUnit.Framework;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentSalesCatalogueServiceTest
    {
        public ISalesCatalogueService fakeSalesCatalogueService;
        public FulfilmentSalesCatalogueService fulfilmentSalesCatalogueService;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";

        [SetUp]
        public void Setup()
        {
            fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();

            fulfilmentSalesCatalogueService = new FulfilmentSalesCatalogueService(fakeSalesCatalogueService);
        }

        #region GetSalesCatalogueDataResponse
        private SalesCatalogueDataResponse GetSalesCatalogueDataResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new List<SalesCatalogueDataProductResponse>()
                {
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
                }
            };
        }
        #endregion

        #region GetSalesCatalogueDataBadrequestResponse
        private SalesCatalogueDataResponse GetSalesCatalogueDataBadrequestResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                ResponseBody = null
            };
        }
        #endregion

        [Test]
        public async Task WhenRequestGetSalesCatalogueDataResponse_ThenReturnsBadrequest()
        {
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueDataBadrequestResponse());

            var response = await fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(fakeBatchId, null);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode);
        }

        [Test]
        public async Task WhenRequestGetSalesCatalogueDataResponse_ThenReturnsDataInResponse()
        {
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored, A<string>.Ignored)).Returns(GetSalesCatalogueDataResponse());

            var response = await fulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(fakeBatchId, null);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.IsInstanceOf(typeof(SalesCatalogueDataResponse), response);
        }
    }
}
