﻿using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
        public IOptions<FileShareServiceConfiguration> fakeFileShareServiceConfig;
        public IOptions<PeriodicOutputServiceConfiguration> fakePeriodicOutputServiceConfiguration;
        public ILogger<FulfilmentAncillaryFiles> fakeLogger;
        public IFileSystemHelper fakeFileSystemHelper;
        public FulfilmentAncillaryFiles fulfilmentAncillaryFiles;
        public string fakeBatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        private readonly string _fakeCorrelationId = "48f53a95-0bd2-4c0c-a6ba-afded2bdffac";
        public string fakeExchangeSetPath = string.Empty;
        public string fakeAioExchangeSetPath = string.Empty;
        public string fakeExchangeSetRootPath = @"F:\\HOME";
        public string fakeFileName = "test.txt";
        readonly FakeFileHelper fakeFileHelper = new FakeFileHelper();
        public string fakeExchangeSetInfoPath = @"C:\\HOME";
        public string fulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";
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
                SerialFileName = "TEST.ENC",
                SerialAioFileName = "TEST.AIO",
                ProductFileName = "PRODUCT.TXT",
                CommentVersion = "VERSION=1.0"
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
                new BatchFile() { Filename = "Test3.000", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>(){ new Attribute() { Key = "s57-CRC", Value = "1234CRC" } } },
                new BatchFile() { Filename = "Test5.001", FileSize = 400, MimeType = "application/s63", Links = new Links { Get = new Link { Href = "" } },
                                Attributes = new List<Attribute>(){ new Attribute() { Key = "s57-CRC", Value = "1234CRC" } } },
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
                        CancelledCellReplacements = new List<string>() { },
                        Compression = true,
                        IssueDateLatestUpdate = DateTime.Today.AddDays(1),
                        LastUpdateNumberPreviousEdition = 0,
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                        IssueDatePreviousUpdate = null
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

        #region GetSalesCatalogueProductResponse
        private SalesCatalogueProductResponse GetSalesCatalogueProductResponse()
        {
            return new SalesCatalogueProductResponse
            {

                Products = new List<Products>() {
                    new Products
                    {
                        ProductName = "10000002",
                        EditionNumber = 10,
                        UpdateNumbers = new List<int?>{3,4},
                        Dates = new List<Dates> {
                            new Dates {UpdateNumber=3, UpdateApplicationDate = DateTime.Today , IssueDate = DateTime.Today },
                        },
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
                        UpdateNumbers = new List<int?>{3,4},
                         Dates = new List<Dates> {
                            new Dates {UpdateNumber=3, IssueDate = DateTime.Today },
                            new Dates {UpdateNumber=4, IssueDate = DateTime.UtcNow },
                        },
                        Cancellation = new Cancellation
                        {
                            EditionNumber= 4,
                            UpdateNumber =3
                        },
                        FileSize = 5300
                    }
                },
                ProductCounts = new ProductCounts
                {
                    RequestedProductCount = 6,
                    RequestedProductsAlreadyUpToDateCount = 8,
                    ReturnedProductCount = 2,
                    RequestedProductsNotReturned = new List<RequestedProductsNotReturned>
                    {
                        new RequestedProductsNotReturned { ProductName = "10000002", Reason = "productWithdrawn" },
                        new RequestedProductsNotReturned { ProductName = "10000003", Reason = "invalidProduct"}
                    }
                }
            };
        }
        #endregion
        [Test]
        public void WhenInvalidCreateSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetInfoPath, null); });
        }

        [Test]
        public async Task WhenValidCreateSerialEncFileRequest_ThenReturnTrueResponse()
        {
            fakeExchangeSetPath = @"C:\\HOME";
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateSerialEncFile(fakeBatchId, fakeExchangeSetPath, null);

            Assert.AreEqual(true, response);
        }

        #region CreateProductFile

        [Test]
        public void WhenInvalidCreateProductFileRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataBadrequestResponse();

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceCatalogueDataNonOkResponse.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service catalogue end point for product.txt responded with {ResponseCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenProductFileIsNotCreatedRequest_ThenReturnFulfilmentException()
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime); });

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ProductFileIsNotCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating sales catalogue data product.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ").MustHaveHappenedOnceExactly();

            Assert.AreEqual(false, fakeFileHelper.CheckAndCreateFolderIsCalled);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WhenValidCreateProductFileRequest_ThenReturnTrueResponseAsync(bool encryption)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            fakeFileHelper.CheckAndCreateFolder(fakeExchangeSetInfoPath);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateProductFile(fakeBatchId, fakeExchangeSetInfoPath, null, salesCatalogueDataResponse, fakeScsRequestDateTime, encryption);

            Assert.AreEqual(true, response);
            Assert.AreEqual(true, fakeFileHelper.CheckAndCreateFolderIsCalled);
        }

        #endregion

        #region CreateCatalogFile

        [Test]
        public async Task WhenValidCreateCatalogFileRequest_ThenReturnTrueReponse()
        {
            byte[] byteContent = new byte[100];
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };

            fakeFileHelper.CheckAndCreateFolder(fakeExchangeSetRootPath);
            fakeFileHelper.CreateFileContentWithBytes(fakeFileName, byteContent);

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);

            var response = await fulfilmentAncillaryFiles.CreateCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.AreEqual(true, response);
            Assert.AreEqual(true, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(true, fakeFileHelper.CreateFileContentWithBytesIsCalled);
            Assert.AreEqual(byteContent, fakeFileHelper.ReadAllBytes(fakeFileName));
        }

        [Test]
        public void WhenInvalidCreateCatalogFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, null, null, null); });
            Assert.AreEqual(false, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(false, fakeFileHelper.CreateFileContentWithBytesIsCalled);
        }

        #endregion

        #region CreateMediaFile
        [Test]
        public void WhenInvalidCreateMediaFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateMediaFile(fakeBatchId, fakeExchangeSetInfoPath, null, "1"); });
        }

        [Test]
        public async Task WhenValidCreateMediaFileRequest_ThenReturnTrueResponse()
        {
            string filePath = @"D:\\Downloads";
            var baseFolder1 = A.Fake<IDirectoryInfo>();
            var baseFolder2 = A.Fake<IDirectoryInfo>();
            A.CallTo(() => baseFolder1.Name).Returns("B1");
            A.CallTo(() => baseFolder2.Name).Returns("B2");
            IDirectoryInfo[] directoryInfos = { baseFolder1, baseFolder2 };
            string[] subdirectoryPaths = { filePath };

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).Returns(directoryInfos);
            A.CallTo(() => fakeFileSystemHelper.GetDirectories(A<string>.Ignored)).Returns(subdirectoryPaths);

            var response = await fulfilmentAncillaryFiles.CreateMediaFile(fakeBatchId, fakeExchangeSetInfoPath, null, "1");

            Assert.AreEqual(true, response);
        }
        #endregion

        #region CreateLargeMediaSerialEncFile
        [Test]
        public void WhenInvalidCreateLargeMediaSerialEncFileRequest_ThenReturnFulfilmentException()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(fakeBatchId, fakeExchangeSetInfoPath, null, "1", "2"); });
        }

        [Test]
        public async Task WhenValidCreateLargeMediaSerialEncFileRequest_ThenReturnTrueResponse()
        {
            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored));
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateLargeMediaSerialEncFile(fakeBatchId, fakeExchangeSetInfoPath, null, "1", "2");

            Assert.AreEqual(true, response);
        }
        #endregion

        #region CreateLargeExchangeSetCatalogFile
        [Test]
        public async Task WhenValidCreateLargeExchangeSetCatalogFileRequest_ThenReturnTrueReponse()
        {
            IDirectoryInfo directoryInfos = A.Fake<IDirectoryInfo>();
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            byte[] byteContent = new byte[100];

            fakeFileHelper.CheckAndCreateFolder(fakeExchangeSetRootPath);
            fakeFileHelper.CreateFileContentWithBytes(fakeFileName, byteContent);

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.ReadAllBytes(A<string>.Ignored)).Returns(byteContent);
            A.CallTo(() => fakeFileSystemHelper.GetParent(A<string>.Ignored)).Returns(directoryInfos);
            var response = await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse);

            Assert.AreEqual(true, response);
            Assert.AreEqual(true, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(true, fakeFileHelper.CreateFileContentWithBytesIsCalled);
            Assert.AreEqual(byteContent, fakeFileHelper.ReadAllBytes(fakeFileName));
        }

        [Test]
        public void WhenInvalidCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            var fulfilmentDataResponse = new List<FulfilmentDataResponse>() {
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000002", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() },
                new FulfilmentDataResponse{ BatchId = "63d38bde-5191-4a59-82d5-aa22ca1cc6dc", EditionNumber = 10, ProductName = "10000003", UpdateNumber = 3, FileUri = new List<string>{ "http://ffs-demo.azurewebsites.net" }, Files= GetFiles() }
            };
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, fulfilmentDataResponse, salesCatalogueDataResponse, salesCatalogueProductResponse); });
            Assert.AreEqual(false, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(false, fakeFileHelper.CreateFileContentWithBytesIsCalled);
        }

        [Test]
        public void WhenNullCreateLargeExchangeSetCatalogFileRequest_ThenReturnFulfilmentException()
        {
            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateLargeExchangeSetCatalogFile(fakeBatchId, fakeExchangeSetRootPath, null, null, null, null); });
            Assert.AreEqual(false, fakeFileHelper.CheckAndCreateFolderIsCalled);
            Assert.AreEqual(false, fakeFileHelper.CreateFileContentWithBytesIsCalled);
        }

        #endregion

        #region CreateEncUpdateCsv

        [Test]
        public void WhenInvalidCreateEncUpdateCsvFileRequest_ThenReturnFulfilmentException()
        {
            string filePath = @"D:\\Downloads";
            TextWriter textWriter = A.Fake<TextWriter>();
            textWriter.Write("Test Stream");

            A.CallTo(() => fakeFileSystemHelper.WriteStream(filePath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                 async delegate { await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), filePath, fakeBatchId, null); });
        }

        [Test]
        public async Task WhenValidCreateEncUpdateCsvFileRequest_ThenReturnTrueResponse()
        {
            string filePath = @"D:\\Downloads";
            TextWriter textWriter = A.Fake<TextWriter>();
            textWriter.Write("Test Stream");

            A.CallTo(() => fakeFileSystemHelper.WriteStream(filePath)).Returns(textWriter);
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true);

            var response = await fulfilmentAncillaryFiles.CreateEncUpdateCsv(GetSalesCatalogueDataResponse(), filePath, fakeBatchId, null);

            Assert.IsTrue(response);
        }

        #endregion

        #region AIO

        [Test]
        public void WhenInvalidCreateSerialAioFileRequest_ThenReturnFulfilmentException()
        {
            bool checkAioSerialFileCreated = false;

            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(true).Once().Then.Returns(checkAioSerialFileCreated);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(fulfilmentExceptionMessage),
                  async delegate { await fulfilmentAncillaryFiles.CreateSerialAioFile(fakeBatchId, fakeExchangeSetRootPath, _fakeCorrelationId, GetSalesCatalogueDataResponse());});

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SerialAioFileIsNotCreated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in creating serial.aio file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} - Invalid Exchange Set Path").MustHaveHappenedOnceExactly();

            Assert.IsFalse(checkAioSerialFileCreated);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_ThenReturnTrueResponse()
        {
            bool checkAioSerialFileCreated = true;
                        
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(checkAioSerialFileCreated).Twice();

            checkAioSerialFileCreated = await fulfilmentAncillaryFiles.CreateSerialAioFile(fakeBatchId, fakeExchangeSetRootPath, _fakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.IsTrue(checkAioSerialFileCreated);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidCreateSerialAioFileRequest_CdTypeUpdate_ThenReturnTrueResponse()
        {
            bool checkAioSerialFileCreated = true;
                        
            A.CallTo(() => fakeFileSystemHelper.CheckFileExists(A<string>.Ignored)).Returns(false).Once().Then.Returns(checkAioSerialFileCreated);

            checkAioSerialFileCreated = await fulfilmentAncillaryFiles.CreateSerialAioFile(fakeBatchId, fakeExchangeSetRootPath, _fakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.IsTrue(checkAioSerialFileCreated);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenEmptyExchangeSetPathCreateSerialAioFileRequest_ThenReturnFalseResponse()
        {
            bool checkAioSerialFileCreated = await fulfilmentAncillaryFiles.CreateSerialAioFile(fakeBatchId, string.Empty, _fakeCorrelationId, GetSalesCatalogueDataResponse());

            Assert.IsFalse(checkAioSerialFileCreated);
            Assert.IsFalse(fakeFileHelper.CheckAndCreateFolderIsCalled);

            A.CallTo(() => fakeFileSystemHelper.CheckAndCreateFolder(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        #endregion
    }
}