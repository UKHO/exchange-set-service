using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public ILogger<FulfilmentDataService> fakeLogger;
        public IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        public FulfilmentAncillaryFiles fulFilmentAncillaryFilesTest;
        public string fakeExchangeSetInfoPath = @"C:\\HOME";

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit = 100, Start = 0, ProductLimit = 4, UpdateNumberLimit = 10, EncRoot = "ENC_ROOT", ExchangeSetFileFolder = "V01X01", ProductFileName = "PRODUCT.TXT" });

            fulFilmentAncillaryFilesTest = new FulfilmentAncillaryFiles(fakeLogger, fakefileShareServiceConfig);
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
                        LastUpdateNumberForPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
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
        public void WhenRequestCreateProductFile_ThenReturnsFalseIfFileIsNotCreated()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();
          
            var response =  fulFilmentAncillaryFilesTest.CreateProductFile(batchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse);

            Assert.AreEqual(false, response);
        }

        [Test]
        public void WhenRequestCreateProductFile_ThenReturnsTrueIfFileIsCreated()
        {
            string batchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            
            var response =  fulFilmentAncillaryFilesTest.CreateProductFile(batchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse);

            Assert.AreEqual(true, response);
        }
    }
}
