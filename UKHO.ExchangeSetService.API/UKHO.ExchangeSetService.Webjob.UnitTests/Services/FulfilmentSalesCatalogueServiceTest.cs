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
        public async Task WhenRequestSalesCatalogueDataResponse_ThenReturnsBadrequest()
        {
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored)).Returns(GetSalesCatalogueDataBadrequestResponse());

            var response = await fulfilmentSalesCatalogueService.CreateSalesCatalogueDataResponse(null);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.ResponseCode);
        }

        [Test]
        public async Task WhenRequestSalesCatalogueDataResponse_ThenReturnsDataInResponse()
        {
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueDataResponse(A<string>.Ignored)).Returns(GetSalesCatalogueDataResponse());

            var response = await fulfilmentSalesCatalogueService.CreateSalesCatalogueDataResponse(null);

            Assert.IsNotNull(response);
            Assert.AreEqual(HttpStatusCode.OK, response.ResponseCode);
            Assert.IsInstanceOf(typeof(SalesCatalogueDataResponse), response);
        }
    }
}
