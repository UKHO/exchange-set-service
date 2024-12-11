using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
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
    public class FulfilmentAncillaryFilesTest
    {
        private IOptions<FileShareServiceConfiguration> _fakeFileShareServiceConfig;
        private ILogger<FulfilmentAncillaryFiles> _fakeLogger;
        private IFileSystemHelper _fakeFileSystemHelper;
        private FulfilmentAncillaryFiles _fulfilmentAncillaryFiles;
        private const string FakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private const string FakeCorrelationId = "48f53a95-0bd2-4c0c-a6ba-afded2bdffac";
        private string _fakeExchangeSetPath = string.Empty;
        private const string FakeExchangeSetRootPath = @"F:\\HOME";
        private const string FakeFileName = "test.txt";
        private readonly FakeFileHelper _fakeFileHelper = new();
        private const string FakeExchangeSetInfoPath = @"C:\\HOME";
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
        private readonly DateTime _fakeScsRequestDateTime = DateTime.UtcNow;

        [SetUp]
        public void Setup()
        {
            _fakeFileShareServiceConfig = Options.Create(new FileShareServiceConfiguration()
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
                SerialAioFileName = "TEST.AIO",
                ProductFileName = "PRODUCT.TXT",
                CommentVersion = "VERSION=1.0"
            });
            _fakeLogger = A.Fake<ILogger<FulfilmentAncillaryFiles>>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            _fulfilmentAncillaryFiles = new FulfilmentAncillaryFiles(_fakeLogger, _fakeFileShareServiceConfig, _fakeFileSystemHelper);
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
            _fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchId, FakeExchangeSetInfoPath, null); });
        }

        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            _fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await _fulfilmentAncillaryFiles.CreateSerialEncFile(FakeBatchId, _fakeExchangeSetPath, null);

            Assert.That(response, Is.EqualTo(true));
        }

        #region CreateProductFile

        [Test]
        public void WhenInvalidCreateProductFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, null, salesCatalogueDataResponse, _fakeScsRequestDateTime); });

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service catalogue end point for product.txt responded with {ResponseCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenProductFileIsNotCreatedRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, null, salesCatalogueDataResponse, _fakeScsRequestDateTime); });

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ProductFileIsNotCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();

            Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(false));
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateProductFileRequest_ThenReturnTrueResponseAsync(bool encryption)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            _fakeFileHelper.CheckAndCreateFolder(FakeExchangeSetInfoPath);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await _fulfilmentAncillaryFiles.CreateProductFile(FakeBatchId, FakeExchangeSetInfoPath, null, salesCatalogueDataResponse, _fakeScsRequestDateTime, encryption);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(true));
            });
        }

        #endregion

        #region CreateCatalogFile

        [Test]
        public async Task WhenValidCreateCatalogFileRequest_ThenReturnTrueReponse()
        {
            var byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse> {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };

            _fakeFileHelper.CheckAndCreateFolder(FakeExchangeSetRootPath);
            _fakeFileHelper.CreateFileContentWithBytes(FakeFileName, byteContent);

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);

            var response = await _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchId, FakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.CreateFileContentWithBytesIsCalled, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.ReadAllBytes(FakeFileName), Is.EqualTo(byteContent));
            });
        }

        [Test]
        public void WhenInvalidCreateCatalogFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateCatalogFile(FakeBatchId, FakeExchangeSetRootPath, null, null, null, null); });
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(false));
                Assert.That(_fakeFileHelper.CreateFileContentWithBytesIsCalled, Is.EqualTo(false));
            });
        }

        #endregion

        #region CreateMediaFile
        [Test]
        public void WhenInvalidCreateMediaFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchId, FakeExchangeSetInfoPath, null, "1"); });
        }

        [Test]
        public async Task WhenValidCreateMediaFileRequest_ThenReturnTrueResponse()
        {
            const string filePath = @"D:\\Downloads";
            var baseFolder1 = A.Fake<IDirectoryInfo>();
            var baseFolder2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseFolder1.Name).Returns("B1");
            A.CallTo(() => baseFolder2.Name).Returns("B2");
            IDirectoryInfo[] directoryInfos = [baseFolder1, baseFolder2];
            string[] subdirectoryPaths = [filePath];

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(A<string>.Ignored)).Returns(subdirectoryPaths);

            var response = await _fulfilmentAncillaryFiles.CreateMediaFile(FakeBatchId, FakeExchangeSetInfoPath, null, "1");

            Assert.That(response, Is.EqualTo(true));
        }
        #endregion

        #region CreateLargeMediaSerialEncFile
        [Test]
        public void WhenInvalidCreateLargeMediaSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchId, FakeExchangeSetInfoPath, null, "1", "2"); });
        }

        [Test]
        public async Task WhenValidCreateLargeMediaSerialEncFileRequest_ThenReturnTrueResponse()
        {
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await _fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(FakeBatchId, FakeExchangeSetInfoPath, null, "1", "2");

            Assert.That(response, Is.EqualTo(true));
        }
        #endregion

        #region CreateLargeExchangeSetCatalogFile
        [Test]
        public async Task WhenValidCreateLargeExchangeSetCatalogFileRequest_ThenReturnTrueReponse()
        {
            var directoryInfos = A.Fake<IDirectoryInfo>();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var byteContent = new byte[100];

            _fakeFileHelper.CheckAndCreateFolder(FakeExchangeSetRootPath);
            _fakeFileHelper.CreateFileContentWithBytes(FakeFileName, byteContent);

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => _fakeFileSystemHelper.GetParent(A<string>.Ignored)).Returns(directoryInfos);
            var response = await _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, FakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.CreateFileContentWithBytesIsCalled, Is.EqualTo(true));
                Assert.That(_fakeFileHelper.ReadAllBytes(FakeFileName), Is.EqualTo(byteContent));
            });
        }

        [Test]
        public void WhenInvalidCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new() { BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, FakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse); });
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(false));
                Assert.That(_fakeFileHelper.CreateFileContentWithBytesIsCalled, Is.EqualTo(false));
            });
        }

        [Test]
        public void WhenNullCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(FakeBatchId, FakeExchangeSetRootPath, null, null, null, null); });
            Assert.Multiple(() =>
            {
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.EqualTo(false));
                Assert.That(_fakeFileHelper.CreateFileContentWithBytesIsCalled, Is.EqualTo(false));
            });
        }

        #endregion

        #region CreateEncUpdateCsv

        [Test]
        public void WhenInvalidCreateEncUpdateCsvFileRequest_ThenReturnFulfilmentException()
        {
            const string filePath = @"D:\\Downloads";
            var textWriter = A.Fake<TextWriter>();
            textWriter.Write("Test Stream");

            A.CallTo(() => _fakeFileSystemHelper.WriteStream(filePath)).Returns(textWriter);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                 async delegate { await _fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), filePath, FakeBatchId, null); });
        }

        [Test]
        public async Task WhenValidCreateEncUpdateCsvFileRequest_ThenReturnTrueResponse()
        {
            const string filePath = @"D:\\Downloads";
            var textWriter = A.Fake<TextWriter>();
            textWriter.Write("Test Stream");

            A.CallTo(() => _fakeFileSystemHelper.WriteStream(filePath)).Returns(textWriter);
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await _fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), filePath, FakeBatchId, null);

            Assert.That(response, Is.True);
        }

        #endregion

        #region AIO

        [Test]
        public void WhenInvalidCreateSerialAioFileRequest_ThenReturnFulfilmentException()
        {
            var checkAioSerialFileCreated = false;

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true).Once().Then.Returns(checkAioSerialFileCreated);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                  async delegate { await _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeExchangeSetRootPath, FakeCorrelationId, GetSalesCatalogueDataResponse()); });

            A.CallTo(_fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SerialAioFileIsNotCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating serial.aio file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path").MustHaveHappenedOnceExactly();

            Assert.That(checkAioSerialFileCreated, Is.False);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_ThenReturnTrueResponse()
        {
            var checkAioSerialFileCreated = true;

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(checkAioSerialFileCreated).Twice();

            checkAioSerialFileCreated = await _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeExchangeSetRootPath, FakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.That(checkAioSerialFileCreated, Is.True);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeUpdate_ThenReturnTrueResponse()
        {
            var checkAioSerialFileCreated = true;

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false).Once().Then.Returns(checkAioSerialFileCreated);

            checkAioSerialFileCreated = await _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, FakeExchangeSetRootPath, FakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.That(checkAioSerialFileCreated, Is.True);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenEmptyExchangeSetPathCreateSerialAioFileRequest_ThenReturnFalseResponse()
        {
            var checkAioSerialFileCreated = await _fulfilmentAncillaryFiles.CreateSerialAioFile(FakeBatchId, string.Empty, FakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.Multiple(() =>
            {
                Assert.That(checkAioSerialFileCreated, Is.False);
                Assert.That(_fakeFileHelper.CheckAndCreateFolderIsCalled, Is.False);
            });

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        #endregion
    }
}
