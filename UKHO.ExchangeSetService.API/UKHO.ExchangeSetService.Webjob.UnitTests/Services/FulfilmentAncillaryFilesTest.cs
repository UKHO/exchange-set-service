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
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentAncillaryFilesTest
    {
        public IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        public ILogger<FulfilmentAncillaryFiles> fakeLogger;
        public IFileSystemHelper fakeFileSystemHelper;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        public string fakeExchangeSetPath = string.Empty;
        public string fakeExchangeSetRootPath = @"F:\\HOME";
        public string fakeFileName = "test.txt";
        readonly FakeFileHelper fakeFileHelper = new FakeFileHelper();
        public string fakeExchangeSetInfoPath = @"C:\\HOME";

        [SetUp]
        public void Setup()
        {
            fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
            {
                BaseUrl = "http://tempuri.org",
                CellName = "DE260001",
                EditionNumber = "1",
                Limit = 10,
                Start = 0,
                ProductCode = "AVCS",
                ProductLimit = 4,
                UpdateNumber = "0",
                UpdateNumberLimit = 10,
                ParallelSearchTaskCount = 10,
                EncRoot = "ENC_ROOT",
                ExchangeSetFileFolder = "V01X01",
                ReadMeFileName = "ReadMe.txt",
                CatalogFileName = "CATALOG.031",
                SerialFileName = "TEST.ENC",
                ProductFileName = "PRODUCT.TXT"
            });
            fakeLogger = A.Fake<ILogger<FulfilmentAncillaryFiles>>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakeLogger, fakeFileShareServiceConfig, fakeFileSystemHelper);
        }

        public List<BatchFile> GetFiles()
        {
            List<BatchFile> batchFiles = new List<BatchFile>
            {
                new BatchFile() { Filename = "Test1.txt", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile() { Filename = "Test2.001", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile() { Filename = "Test3.000", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile() { Filename = "TEST4.TIF", FileSize = 400, MimeType = "IMAGE/TIFF", Links = new Links { Get = new Link { Href = "" } } },
                new BatchFile() { Filename = "Default.img", FileSize = 400, MimeType = "image/jpeg", Links = new Links { Get = new Link { Href = "" } } }
            };
            return batchFiles;
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
            fakeFileHelper.CheckAndCreateFolder(fakeExchangeSetInfoPath);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse);

            Assert.AreEqual(true, response);
            Assert.AreEqual(true, fakeFileHelper.CheckAndCreateFolderIsCalled);
        }

        #endregion

        #region CreateCatalogFile

        [Test]
        public async Task WhenValidCreateCatalogFileRequest_ThenReturnTrueReponse()
        {
            byte[] byteContent = new byte[100];
            var fulfilmentDataResponses = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "Demo", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };

            fakeFileHelper.CheckAndCreateFolder(fakeExchangeSetRootPath);
            fakeFileHelper.CreateFileContentWithBytes(fakeFileName, byteContent);

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, fulfilmentDataResponses);

            Assert.AreEqual(true, response);
            Assert.AreEqual(true, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(true, fakeFileHelper.CreateFileContentWithBytesIsCalled);
        }

        [Test]
        public async Task WhenInvalidCreateCatalogFileRequest_ThenReturnFalseReponse()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, null);

            Assert.AreEqual(false, response);
            Assert.AreEqual(false, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(false, fakeFileHelper.CreateFileContentWithBytesIsCalled);
        }

        #endregion
    }
}