using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public IOptions<FileShareServiceConfiguration> fakefileShareServiceConfig;
        public IFileSystemHelper fakeFileSystemHelper;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        public ILogger<FulfilmentDataService> fakeLogger;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        public string fakeExchangeSetPath = string.Empty;
        public string fakeExchangeSetInfoPath = @"C:\\HOME";

        [SetUp]
        public void Setup()
        {
            fakefileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            { Limit = 100, Start = 0, ProductLimit = 4, UpdateNumberLimit = 10, EncRoot = "ENC_ROOT", ExchangeSetFileFolder = "V01X01", ProductFileName = "PRODUCT.TXT", SerialFileName = "TEST.ENC" });
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();

            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakefileShareServiceConfig, fakeFileSystemHelper, fakeLogger);
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
        public async Task WhenInvalidCreateSerialEncFileRequest_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(false, response);
        }

        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(true, response);
        }

        #region CreateProductFile
        [Test]
        public void WhenInvalidCreateProductFileRequest_ThenReturnFalseResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();

            var response = fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse);

            Assert.AreEqual(false, response);
        }

        [Test]
        public void WhenValidCreateProductFileRequest_ThenReturnTrueResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse);

            Assert.AreEqual(true, response);
        }
        #endregion
    }
}