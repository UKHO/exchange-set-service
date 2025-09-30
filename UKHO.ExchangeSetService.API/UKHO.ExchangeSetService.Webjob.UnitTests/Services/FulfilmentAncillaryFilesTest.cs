using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using Attribute = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Attribute;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public partial class FulfilmentAncillaryFilesTest
    {
        private IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        private ILogger<FulfilmentAncillaryFiles> fakeLogger;
        private IFileSystemHelper fakeFileSystemHelper;
        private FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        private const string FakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private const string FakeCorrelationId = "48f53a95-0bd2-4c0c-a6ba-afded2bdffac";
        private const string FakeBatchPath = $@"C:\HOME\25SEP2025\{FakeBatchId}";
        private const string FakeExchangeSetPath = $@"{FakeBatchPath}\V01X01";
        private const string FakeExchangeSetEncRootPath = $@"{FakeExchangeSetPath}\ENC_ROOT";
        private const string FakeExchangeSetInfoPath = $@"{FakeExchangeSetPath}\INFO";
        private const string FakeExchangeSetMediaBaseNumber = "5";
        private const string FakeExchangeSetMediaPath = $@"{FakeBatchPath}\M0{FakeExchangeSetMediaBaseNumber}X02";
        private const string FakeExchangeSetMediaInfoPath = $@"{FakeBatchPath}\M0{FakeExchangeSetMediaBaseNumber}X02\INFO";
        private const string FakeExchangeSetMediaFilePath = $@"{FakeExchangeSetMediaPath}\MEDIA.TXT";
        private const string FakeSerialFilePath = $@"{FakeExchangeSetPath}\SERIAL.ENC";
        private const string FakeProductFilePath = $@"{FakeExchangeSetInfoPath}\PRODUCT.TXT";
        private const string FakeReadMeFilePath = $@"{FakeExchangeSetEncRootPath}\ReadMe.txt";
        private const string FakeCatalogFilePath = $@"{FakeExchangeSetEncRootPath}\CATALOG.031";
        private const string FakeAioExchangeSetPath = $@"{FakeBatchPath}\AIO";
        private const string FakeAioExchangeSetEncRootPath = $@"{FakeAioExchangeSetPath}\ENC_ROOT";
        private const string FakeSerialAioFilePath = $@"{FakeAioExchangeSetPath}\SERIAL.AIO";
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        private readonly DateTime fakeScsRequestDateTime = DateTime.UtcNow;

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
                SerialFileName = "SERIAL.ENC",
                SerialAioFileName = "SERIAL.AIO",
                ProductFileName = "PRODUCT.TXT",
                CommentVersion = "VERSION=1.0",
                Info = "INFO",
                AioExchangeSetFileFolder = "AIO",
            });
            fakeLogger = A.Fake<ILogger<FulfilmentAncillaryFiles>>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakeLogger, fakeFileShareServiceConfig, fakeFileSystemHelper);
        }

        private static List<BatchFile> GetFiles()
        {
            var batchFiles = new List<BatchFile>
            {
                new() { Filename = "Test1.txt", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new() { Filename = "Test2.001", FileSize = 400, MimeType = "text/plain", Links = new Links { Get = new Link { Href = "" } } },
                new() { Filename = "Test3.000", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>{ new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new() { Filename = "Test5.001", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>{ new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new() { Filename = "Test6.000", FileSize = 400, MimeType = "application/s57", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>{new() { Key = "s57-CRC", Value = "1234CRC" } } },
                new() { Filename = "TEST4.TIF", FileSize = 400, MimeType = "IMAGE/TIFF", Links = new Links { Get = new Link { Href = "" } } },
                new() { Filename = "Default.img", FileSize = 400, MimeType = "image/jpeg", Links = new Links { Get = new Link { Href = "" } } }
            };
            return batchFiles;
        }

        #region GetSalesCatalogueDataResponse
        private static SalesCatalogueDataResponse GetSalesCatalogueDataResponse() => new()
        {
            ResponseCode = HttpStatusCode.OK,
            ResponseBody =
                [
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
                        CancelledCellReplacements = [],
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today,
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                        IssueDatePreviousUpdate = DateTime.Today.AddDays(-1)
                    },
                    new SalesCatalogueDataProductResponse
                    {
                        ProductName = "10000003",
                        LatestUpdateNumber = 0,
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
                        CancelledCellReplacements = [],
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today.AddDays(1),
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                        IssueDatePreviousUpdate = null
                    }
                ]
        };
        #endregion

        #region GetSalesCatalogueDataBadrequestResponse
        private static SalesCatalogueDataResponse GetSalesCatalogueDataBadrequestResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.BadRequest,
                ResponseBody = null
            };
        }
        #endregion

        #region GetSalesCatalogueProductResponse
        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponse()
        {
            return new SalesCatalogueProductResponse
            {

                Products = [
                    new Products
                    {
                        ProductName = "10000002",
                        EditionNumber = 10,
                        UpdateNumbers = [3,4],
                        Dates = [
                            new Dates {UpdateNumber=3, UpdateApplicationDate = DateTime.Today , IssueDate = DateTime.Today },
                        ],
                        Cancellation = new Cancellation
                        {
                            EditionNumber= 9,
                            UpdateNumber =3
                        },
                        FileSize = 2800
                    },
                    new Products
                    {
                        ProductName = "10000003",
                        EditionNumber = 10,
                        UpdateNumbers = [3,4],
                         Dates = [
                            new Dates {UpdateNumber=3, IssueDate = DateTime.Today },
                            new Dates {UpdateNumber=4, IssueDate = DateTime.UtcNow },
                        ],
                        Cancellation = new Cancellation
                        {
                            EditionNumber= 4,
                            UpdateNumber =3
                        },
                        FileSize = 5300
                    }
                ],
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 6,
                    RequestedProductsAlreadyUpToDateCount = 8,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned =
                    [
                        new RequestedProductsNotReturned { ProductName = "10000002", Reason = "productWithdrawn" },
                        new RequestedProductsNotReturned { ProductName = "10000003", Reason = "invalidProduct"}
                    ]
                }
            };
        }
        #endregion

        [Test]
        public void WhenInvalidCreateSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchId, FakeExchangeSetInfoPath, null); });
        }

        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetPath));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeSerialFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchId, FakeExchangeSetPath, FakeCorrelationId);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeSerialFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialFilePath)).MustHaveHappenedOnceExactly();
        }

        #region CreateProductFile

        [Test]
        public void WhenInvalidCreateProductFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service catalogue end point for product.txt responded with {ResponseCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public void WhenProductFileIsNotCreatedRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ProductFileIsNotCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateProductFileRequest_ThenReturnTrueResponseAsync(bool encryption)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetInfoPath));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeProductFilePath, A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, FakeCorrelationId, salesCatalogueDataResponse, fakeScsRequestDateTime, encryption);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetInfoPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeProductFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateCatalogFile

        [Test]
        public async Task WhenValidCreateCatalogFileRequest_ThenReturnTrueReponse()
        {
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeReadMeFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeCatalogFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);

            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchId, FakeExchangeSetEncRootPath, FakeCorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeReadMeFilePath)).MustHaveHappenedOnceExactly();

            foreach (var item in fulfilmentDataResponse)
            {
                foreach (var file in item.Files.Where(x => x.MimeType != "application/s57" && x.MimeType != "application/s63"))
                {
                    var filePath = Path.Combine(FakeExchangeSetEncRootPath, item.ProductName.Substring(0, 2), item.ProductName, item.EditionNumber.ToString(), item.UpdateNumber.ToString(), file.Filename);
                    A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(filePath)).MustHaveHappenedOnceExactly();
                }
            }

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetEncRootPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(FakeCatalogFilePath, A<byte[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeCatalogFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidCreateCatalogFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeReadMeFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeCatalogFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchId, FakeExchangeSetEncRootPath, FakeCorrelationId, null, null, null); });

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeReadMeFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetEncRootPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(FakeCatalogFilePath, A<byte[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeCatalogFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateMediaFile

        [Test]
        public void WhenInvalidCreateMediaFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeExchangeSetMediaFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeExchangeSetMediaFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchId, FakeExchangeSetMediaPath, FakeCorrelationId, FakeExchangeSetMediaBaseNumber); });
        }

        [Test]
        public async Task WhenInvalidCreateMediaFileRequest_WithEmptyPath_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchId, string.Empty, FakeCorrelationId, FakeExchangeSetMediaBaseNumber);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenValidCreateMediaFileRequest_ThenReturnTrueResponse()
        {
            var baseFolderPath1 = Path.Combine(FakeExchangeSetMediaPath, "B1");
            var baseFolderPath2 = Path.Combine(FakeExchangeSetMediaPath, "B2");
            var baseEncFolderPath1 = Path.Combine(baseFolderPath1, "ENC_ROOT");
            var baseEncFolderPath2 = Path.Combine(baseFolderPath2, "ENC_ROOT");
            var baseFolder1 = A.Fake<IDirectoryInfo>();
            var baseFolder2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseFolder1.Name).Returns("B1");
            A.CallTo(() => baseFolder2.Name).Returns("B2");
            A.CallTo(() => baseFolder1.ToString()).Returns(baseFolderPath1);
            A.CallTo(() => baseFolder2.ToString()).Returns(baseFolderPath2);
            IDirectoryInfo[] baseFolders = [baseFolder1, baseFolder2];
            string[] subdirectoryPaths1 = [Path.Combine(baseEncFolderPath1, "GB"), Path.Combine(baseEncFolderPath1, "FR")];
            string[] subdirectoryPaths2 = [Path.Combine(baseEncFolderPath2, "DE")];
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeExchangeSetMediaPath)).Returns(baseFolders);
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath1)).Returns(subdirectoryPaths1);
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath2)).Returns(subdirectoryPaths2);
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeExchangeSetMediaFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeExchangeSetMediaFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchId, FakeExchangeSetMediaPath, FakeCorrelationId, FakeExchangeSetMediaBaseNumber);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeExchangeSetMediaPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeExchangeSetMediaPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeExchangeSetMediaFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeExchangeSetMediaFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateLargeMediaSerialEncFile

        [Test]
        public void WhenInvalidCreateLargeMediaSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            var baseFolderPath = Path.Combine(FakeExchangeSetMediaPath, "B1");
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchId, baseFolderPath, FakeCorrelationId, "1", "2"); });
        }

        [Test]
        public async Task WhenInvalidCreateLargeMediaSerialEncFileRequest_WithEmptyPath_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchId, string.Empty, FakeCorrelationId, "1", "2");

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenValidCreateLargeMediaSerialEncFileRequest_ThenReturnTrueResponse()
        {
            var baseFolderPath = Path.Combine(FakeExchangeSetMediaPath, "B1");
            var serialFilePath = Path.Combine(baseFolderPath, "SERIAL.ENC");
            var baseNumber = "1";
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(serialFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(serialFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchId, baseFolderPath, FakeCorrelationId, baseNumber, "2");

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(baseFolderPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(serialFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(serialFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateLargeExchangeSetCatalogFile

        [Test]
        public async Task WhenValidCreateLargeExchangeSetCatalogFileRequest_ThenReturnTrueReponse()
        {
            var b1Path = Path.Combine(FakeExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, "ENC_ROOT");
            var readMeFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.CatalogFileName);
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };
            var directoryInfo = A.Fake<IDirectoryInfo>();
            A.CallTo(() => directoryInfo.Name).Returns($"M0{FakeExchangeSetMediaBaseNumber}X02");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => fakeFileSystemHelper.GetParent(b1Path)).Returns(directoryInfo);

            var response = await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, exchangeSetRootPath, FakeCorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).MustHaveHappenedOnceExactly();

            foreach (var item in fulfilmentDataResponse)
            {
                foreach (var file in item.Files.Where(x => x.MimeType != "application/s57" && x.MimeType != "application/s63"))
                {
                    var filePath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, 2), item.ProductName, item.EditionNumber.ToString(), file.Filename);
                    A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(filePath)).MustHaveHappenedOnceExactly();
                }
            }

            A.CallTo(() => fakeFileSystemHelper.GetParent(b1Path)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(outputFileName, A<byte[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            var b1Path = Path.Combine(FakeExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, "ENC_ROOT");
            var readMeFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.CatalogFileName);
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };
            var directoryInfo = A.Fake<IDirectoryInfo>();
            A.CallTo(() => directoryInfo.Name).Returns($"M0{FakeExchangeSetMediaBaseNumber}X02");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => fakeFileSystemHelper.GetParent(b1Path)).Returns(directoryInfo);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, exchangeSetRootPath, FakeCorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse); });

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenNullCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            var b1Path = Path.Combine(FakeExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, "ENC_ROOT");
            var readMeFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, fakeFileShareServiceConfig.Value.CatalogFileName);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, exchangeSetRootPath, FakeCorrelationId, null, null, null); });

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetParent(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(A<string>.Ignored, A<byte[]>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).MustNotHaveHappened();
        }

        #endregion

        #region CreateEncUpdateCsv

        [Test]
        public void WhenInvalidCreateEncUpdateCsvFileRequest_ThenReturnFulfilmentException()
        {
            var updateListPath = Path.Combine(FakeExchangeSetMediaInfoPath, "ENC Update List.csv");
            var textWriter = A.Fake<TextWriter>();
            A.CallTo(() => fakeFileSystemHelper.WriteStream(updateListPath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(updateListPath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), FakeExchangeSetMediaInfoPath, FakeBatchId, FakeCorrelationId); });

            A.CallTo(() => fakeFileSystemHelper.WriteStream(updateListPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(updateListPath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateEncUpdateCsvFileRequest_ThenReturnTrueResponse()
        {
            var updateListPath = Path.Combine(FakeExchangeSetMediaInfoPath, "ENC Update List.csv");
            var textWriter = A.Fake<TextWriter>();
            A.CallTo(() => fakeFileSystemHelper.WriteStream(updateListPath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(updateListPath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), FakeExchangeSetMediaInfoPath, FakeBatchId, FakeCorrelationId);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.WriteStream(updateListPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(updateListPath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region AIO

        [Test]
        public void WhenInvalidCreateSerialAioFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId, salesCatalogueDataResponse); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.SerialAioFileIsNotCreated.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating serial.aio file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path").MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeAioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeSerialAioFilePath, A<string>.That.Matches(x => BaseRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeBase_ThenReturnTrueResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId, salesCatalogueDataResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeAioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeSerialAioFilePath, A<string>.That.Matches(x => BaseRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeUpdate_ThenReturnTrueResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeAioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeAioExchangeSetPath, FakeCorrelationId, salesCatalogueDataResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeAioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeSerialAioFilePath, A<string>.That.Matches(x => UpdateRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeSerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenEmptyExchangeSetPathCreateSerialAioFileRequest_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, string.Empty, FakeCorrelationId, null);

            Assert.That(response, Is.False);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).MustNotHaveHappened();
        }

        [GeneratedRegex(@"GBWK\d{2}-\d{2}   \d{8}BASE      \d{2}[.]00\x0b\x0d\x0a")]
        private static partial Regex BaseRegex();

        [GeneratedRegex(@"GBWK\d{2}-\d{2}   \d{8}UPDATE    \d{2}[.]00\x0b\x0d\x0a")]
        private static partial Regex UpdateRegex();

        #endregion
    }
}
