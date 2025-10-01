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
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;
using Attribute = UKHO.ExchangeSetService.Common.Models.FileShareService.Response.Attribute;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public partial class FulfilmentAncillaryFilesTest
    {
        private ILogger<FulfilmentAncillaryFiles> fakeLogger;
        private IFileSystemHelper fakeFileSystemHelper;
        private FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        private readonly DateTime fakeScsRequestDateTime = DateTime.UtcNow;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<FulfilmentAncillaryFiles>>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(fakeLogger, FakeBatchValue.FileShareServiceConfiguration, fakeFileSystemHelper);
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
                async delegate { await fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, null); });
        }

        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetPath));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.SerialFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.SerialFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialFilePath)).MustHaveHappenedOnceExactly();
        }

        #region CreateProductFile

        [Test]
        public void WhenInvalidCreateProductFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

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
                async delegate { await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

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
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetInfoPath));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ProductFilePath, A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateProductFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetInfoPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse, fakeScsRequestDateTime, encryption);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetInfoPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ProductFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
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
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ReadMeFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.CatalogFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);

            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ReadMeFilePath)).MustHaveHappenedOnceExactly();

            foreach (var item in fulfilmentDataResponse)
            {
                foreach (var file in item.Files.Where(x => x.MimeType != "application/s57" && x.MimeType != "application/s63"))
                {
                    var filePath = Path.Combine(FakeBatchValue.ExchangeSetEncRootPath, item.ProductName.Substring(0, 2), item.ProductName, item.EditionNumber.ToString(), item.UpdateNumber.ToString(), file.Filename);
                    A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(filePath)).MustHaveHappenedOnceExactly();
                }
            }

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetEncRootPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(FakeBatchValue.CatalogFilePath, A<byte[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.CatalogFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidCreateCatalogFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ReadMeFilePath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.CatalogFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetEncRootPath, FakeBatchValue.CorrelationId, null, null, null); });

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ReadMeFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetEncRootPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContentWithBytes(FakeBatchValue.CatalogFilePath, A<byte[]>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.CatalogFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateMediaFile

        [Test]
        public void WhenInvalidCreateMediaFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ExchangeSetMediaFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ExchangeSetMediaFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetMediaPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetMediaBaseNumber); });
        }

        [Test]
        public async Task WhenInvalidCreateMediaFileRequest_WithEmptyPath_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, string.Empty, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetMediaBaseNumber);

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenValidCreateMediaFileRequest_ThenReturnTrueResponse()
        {
            var baseFolderPath1 = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            var baseFolderPath2 = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B2");
            var baseEncFolderPath1 = Path.Combine(baseFolderPath1, FakeBatchValue.EncRoot);
            var baseEncFolderPath2 = Path.Combine(baseFolderPath2, FakeBatchValue.EncRoot);
            var baseFolder1 = A.Fake<IDirectoryInfo>();
            var baseFolder2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseFolder1.Name).Returns("B1");
            A.CallTo(() => baseFolder2.Name).Returns("B2");
            A.CallTo(() => baseFolder1.ToString()).Returns(baseFolderPath1);
            A.CallTo(() => baseFolder2.ToString()).Returns(baseFolderPath2);
            IDirectoryInfo[] baseFolders = [baseFolder1, baseFolder2];
            string[] subdirectoryPaths1 = [Path.Combine(baseEncFolderPath1, "GB"), Path.Combine(baseEncFolderPath1, "FR")];
            string[] subdirectoryPaths2 = [Path.Combine(baseEncFolderPath2, "DE")];
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.ExchangeSetMediaPath)).Returns(baseFolders);
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath1)).Returns(subdirectoryPaths1);
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath2)).Returns(subdirectoryPaths2);
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ExchangeSetMediaFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ExchangeSetMediaFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetMediaPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetMediaBaseNumber);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.ExchangeSetMediaPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.ExchangeSetMediaPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(baseEncFolderPath2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ExchangeSetMediaFilePath, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ExchangeSetMediaFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region CreateLargeMediaSerialEncFile

        [Test]
        public void WhenInvalidCreateLargeMediaSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            var baseFolderPath = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseFolderPath, FakeBatchValue.CorrelationId, "1", "2"); });
        }

        [Test]
        public async Task WhenInvalidCreateLargeMediaSerialEncFileRequest_WithEmptyPath_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, string.Empty, FakeBatchValue.CorrelationId, "1", "2");

            Assert.That(response, Is.False);
        }

        [Test]
        public async Task WhenValidCreateLargeMediaSerialEncFileRequest_ThenReturnTrueResponse()
        {
            var baseFolderPath = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            var serialFilePath = Path.Combine(baseFolderPath, FakeBatchValue.SerialFileName);
            var baseNumber = "1";
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(serialFilePath, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(serialFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchValue.BatchId, baseFolderPath, FakeBatchValue.CorrelationId, baseNumber, "2");

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
            var b1Path = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, FakeBatchValue.EncRoot);
            var readMeFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.CatalogFileName);
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };
            var directoryInfo = A.Fake<IDirectoryInfo>();
            A.CallTo(() => directoryInfo.Name).Returns($"M0{FakeBatchValue.ExchangeSetMediaBaseNumber}X02");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => fakeFileSystemHelper.GetParent(b1Path)).Returns(directoryInfo);

            var response = await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchValue.BatchId, exchangeSetRootPath, FakeBatchValue.CorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

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
            var b1Path = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, FakeBatchValue.EncRoot);
            var readMeFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.CatalogFileName);
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>
            {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string> { "http://ffs-demo.azurewebsites.net" }, Files = GetFiles() }
            };
            var directoryInfo = A.Fake<IDirectoryInfo>();
            A.CallTo(() => directoryInfo.Name).Returns($"M0{FakeBatchValue.ExchangeSetMediaBaseNumber}X02");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => fakeFileSystemHelper.GetParent(b1Path)).Returns(directoryInfo);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchValue.BatchId, exchangeSetRootPath, FakeBatchValue.CorrelationId, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse); });

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(outputFileName)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenNullCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            var b1Path = Path.Combine(FakeBatchValue.ExchangeSetMediaPath, "B1");
            var exchangeSetRootPath = Path.Combine(b1Path, FakeBatchValue.EncRoot);
            var readMeFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.ReadMeFileName);
            var outputFileName = Path.Combine(exchangeSetRootPath, FakeBatchValue.CatalogFileName);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(readMeFileName)).Returns(true);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchValue.BatchId, exchangeSetRootPath, FakeBatchValue.CorrelationId, null, null, null); });

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
            var textWriter = A.Fake<TextWriter>();
            A.CallTo(() => fakeFileSystemHelper.WriteStream(FakeBatchValue.UpdateListFilePath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.UpdateListFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), FakeBatchValue.ExchangeSetMediaInfoPath, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId); });

            A.CallTo(() => fakeFileSystemHelper.WriteStream(FakeBatchValue.UpdateListFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.UpdateListFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateEncUpdateCsvFileRequest_ThenReturnTrueResponse()
        {
            var textWriter = A.Fake<TextWriter>();
            A.CallTo(() => fakeFileSystemHelper.WriteStream(FakeBatchValue.UpdateListFilePath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.UpdateListFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), FakeBatchValue.ExchangeSetMediaInfoPath, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.WriteStream(FakeBatchValue.UpdateListFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.UpdateListFilePath)).MustHaveHappenedOnceExactly();
        }

        #endregion

        #region AIO

        [Test]
        public void WhenInvalidCreateSerialAioFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.SerialAioFileIsNotCreated.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating serial.aio file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path").MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.AioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.SerialAioFilePath, A<string>.That.Matches(x => BaseRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeBase_ThenReturnTrueResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.AioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.SerialAioFilePath, A<string>.That.Matches(x => BaseRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeUpdate_ThenReturnTrueResponse()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var typeCheckPath1 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000002", "3", "0", "10000002.000");
            var typeCheckPath2 = Path.Combine(FakeBatchValue.AioExchangeSetEncRootPath, "10", "10000003", "3", "0", "10000003.000");
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).Returns(false);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId, salesCatalogueDataResponse);

            Assert.That(response, Is.True);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.AioExchangeSetPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(FakeBatchValue.SerialAioFilePath, A<string>.That.Matches(x => UpdateRegex().Match(x).Success))).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath1)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(typeCheckPath2)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(FakeBatchValue.SerialAioFilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenEmptyExchangeSetPathCreateSerialAioFileRequest_ThenReturnFalseResponse()
        {
            var response = await fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchValue.BatchId, string.Empty, FakeBatchValue.CorrelationId, null);

            Assert.That(response, Is.False);
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).MustNotHaveHappened();
        }

        [GeneratedRegex(@"GBWK\d{2}-\d{2}   \d{8}BASE      \d{2}[.]00\x0b\x0d\x0a")]
        private static partial Regex BaseRegex();

        [GeneratedRegex(@"GBWK\d{2}-\d{2}   \d{8}UPDATE    \d{2}[.]00\x0b\x0d\x0a")]
        private static partial Regex UpdateRegex();

        #endregion AIO
    }
}
