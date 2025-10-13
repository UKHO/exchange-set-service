using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.FileBuilders;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTest
    {
        private FulfilmentDataService fulfilmentDataService;
        private IAzureBlobStorageService fakeAzureBlobStorageService;
        private IFulfilmentFileShareService fakeFulfilmentFileShareService;
        private ILogger<FulfilmentDataService> fakeLogger;
        private IConfiguration fakeConfiguration;
        private IFulfilmentSalesCatalogueService fakeFulfilmentSalesCatalogueService;
        private IFulfilmentCallBackService fakeFulfilmentCallBackService;
        private IExchangeSetBuilder fakeExchangeSetBuilder;
        private IMonitorHelper fakeMonitorHelper;
        private IFileSystemHelper fakeFileSystemHelper;
        private const string FulfilmentExceptionMessage = "There has been a problem in creating your exchange set, so we are unable to fulfil your request at this time. Please contact UKHO Customer Services quoting error code : {0} and correlation ID : {1}";

        [SetUp]
        public void Setup()
        {
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            fakeFulfilmentFileShareService = A.Fake<IFulfilmentFileShareService>();
            fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeConfiguration["HOME"] = FakeBatchValue.HomeDirectoryPath;
            fakeFulfilmentSalesCatalogueService = A.Fake<IFulfilmentSalesCatalogueService>();
            fakeFulfilmentCallBackService = A.Fake<IFulfilmentCallBackService>();
            fakeMonitorHelper = A.Fake<IMonitorHelper>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeExchangeSetBuilder = A.Fake<IExchangeSetBuilder>();
            fulfilmentDataService = new FulfilmentDataService(fakeAzureBlobStorageService, fakeFulfilmentFileShareService, fakeLogger, FakeBatchValue.FileShareServiceConfiguration, fakeConfiguration, fakeFulfilmentSalesCatalogueService, fakeFulfilmentCallBackService, fakeMonitorHelper, FakeBatchValue.AioConfiguration, fakeFileSystemHelper, fakeExchangeSetBuilder);
        }

        #region GetScsResponseQueueMessage

        private static SalesCatalogueServiceResponseQueueMessage GetScsResponseQueueMessage(string exchangeSetStandard)
        {
            return new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                FileSize = 4000,
                ScsResponseUri = $"https://test/ess-test/{FakeBatchValue.BatchId}.json",
                CallbackUri = "https://test-callbackuri.com",
                CorrelationId = FakeBatchValue.CorrelationId,
                IsEmptyEncExchangeSet = false,
                IsEmptyAioExchangeSet = false,
                ExchangeSetStandard = exchangeSetStandard
            };
        }

        #endregion

        #region GetSalesCatalogueResponse

        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponse(bool includeAio = false)
        {
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse
            {
                Products =
                [
                    new Products { ProductName = "GB800002", EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 },
                    new Products { ProductName = "GB800003", EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 }
                ]
            };

            if (includeAio)
            {
                salesCatalogueProductResponse.Products.Add(new Products { ProductName = FakeBatchValue.AioCell1, EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 });
            }

            return salesCatalogueProductResponse;
        }

        private static SalesCatalogueProductResponse GetSalesCatalogueProductResponseAioOnly()
        {
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse
            {
                Products =
                [
                    new Products { ProductName = FakeBatchValue.AioCell1, EditionNumber = 1, UpdateNumbers = [0], FileSize = 100 }
                ]
            };

            return salesCatalogueProductResponse;
        }

        #endregion

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
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,"
                    }
                ]
            };
        }

        private static SalesCatalogueDataResponse GetSalesCatalogueDataResponseForAio()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody =
                [
                    new SalesCatalogueDataProductResponse
                    {
                        ProductName = FakeBatchValue.AioCell1,
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
                        TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,"
                    }
                ]
            };
        }

        #endregion

        private static IDirectoryInfo[] GetSubDirectories(bool standard, bool aio, bool large)
        {
            var count = 0;
            count += standard ? 1 : 0;
            count += aio ? 1 : 0;
            count += large ? 1 : 0;
            var fakeDirectoryInfos = new IDirectoryInfo[count];
            var currentEntry = 0;

            if (standard)
            {
                var fakeStandardDirectoryInfo = A.Fake<IDirectoryInfo>();
                A.CallTo(() => fakeStandardDirectoryInfo.Name).Returns(FakeBatchValue.ExchangeSetFileFolder);
                A.CallTo(() => fakeStandardDirectoryInfo.FullName).Returns(FakeBatchValue.ExchangeSetPath);
                A.CallTo(() => fakeStandardDirectoryInfo.ToString()).Returns(FakeBatchValue.ExchangeSetPath);
                fakeDirectoryInfos[currentEntry] = fakeStandardDirectoryInfo;
                currentEntry++;
            }

            if (aio)
            {
                var fakeAioDirectoryInfo = A.Fake<IDirectoryInfo>();
                A.CallTo(() => fakeAioDirectoryInfo.Name).Returns(FakeBatchValue.AioExchangeSetFileFolder);
                A.CallTo(() => fakeAioDirectoryInfo.FullName).Returns(FakeBatchValue.AioExchangeSetPath);
                A.CallTo(() => fakeAioDirectoryInfo.ToString()).Returns(FakeBatchValue.AioExchangeSetPath);
                fakeDirectoryInfos[currentEntry] = fakeAioDirectoryInfo;
                currentEntry++;
            }

            if (large)
            {
                var fakeLargeDirectoryInfo = A.Fake<IDirectoryInfo>();
                A.CallTo(() => fakeLargeDirectoryInfo.Name).Returns(FakeBatchValue.LargeExchangeSetFolderName5);
                A.CallTo(() => fakeLargeDirectoryInfo.FullName).Returns(FakeBatchValue.LargeExchangeSetMediaPath5);
                A.CallTo(() => fakeLargeDirectoryInfo.ToString()).Returns(FakeBatchValue.LargeExchangeSetMediaPath5);
                fakeDirectoryInfos[currentEntry] = fakeLargeDirectoryInfo;
            }

            return fakeDirectoryInfos;
        }

        private static IFileInfo[] GetZipFiles(bool standard, bool aio)
        {
            var count = 0;
            count += standard ? 1 : 0;
            count += aio ? 1 : 0;
            var fakeFileInfos = new IFileInfo[count];
            var currentEntry = 0;

            if (standard)
            {
                var fakeStandardFileInfo = A.Fake<IFileInfo>();
                A.CallTo(() => fakeStandardFileInfo.Name).Returns(FakeBatchValue.ExchangeSetZipFileName);
                A.CallTo(() => fakeStandardFileInfo.FullName).Returns(FakeBatchValue.ExchangeSetZipFilePath);
                fakeFileInfos[currentEntry] = fakeStandardFileInfo;
                currentEntry++;
            }

            if (aio)
            {
                var fakeAioFileInfo = A.Fake<IFileInfo>();
                A.CallTo(() => fakeAioFileInfo.Name).Returns(FakeBatchValue.AioExchangeSetZipFileName);
                A.CallTo(() => fakeAioFileInfo.FullName).Returns(FakeBatchValue.AioExchangeSetZipFilePath);
                fakeFileInfos[currentEntry] = fakeAioFileInfo;
            }

            return fakeFileInfos;
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        [TestCase("s57", FakeBatchValue.S57BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStandardExchangeSetNoAioCreatedSuccessfully(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        [TestCase("s57", FakeBatchValue.S57BusinessUnit)]
        public void WhenValidMessageQueueTriggerAndZipFolderNotCreated_ThenThrowsFulfilmentException(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate); });

            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ErrorInCreatingZipFile, "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", logLevel: LogLevel.Error);
        }

        [Test]
        [TestCase("test")]
        public void WhenExchangeSetStandardParameterOtherThanS63AndS57_ThenThrowsException(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate); });
        }

        #region AIO

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetCreatedSuccessfully(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, true));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating ENC exchange set without AIO exchange set")]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_WithNoAioCellsConfigured_ThenReturnsExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard, string businessUnit)
        {
            FakeBatchValue.AioConfiguration.Value.AioCells = string.Empty; // no AIO cells configured
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.AioExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.AioExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating Empty Aio exchange set without ENC exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyAioExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponseForAio();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            message.IsEmptyAioExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = [] };
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(false, true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(false, true));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<SalesCatalogueProductResponse>.Ignored, A<List<Products>>.Ignored, A<string>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.ExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.ExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating Empty ENC exchange set without AIO exchange set")]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsEmptyExchangeSetCreatedSuccessfullyWithoutAioCell(string exchangeSetStandard, string businessUnit)
        {
            FakeBatchValue.AioConfiguration.Value.AioCells = null; // no AIO cells configured
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            message.IsEmptyEncExchangeSet = true;
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse { Products = [] };
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, false, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Created Successfully"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.AioExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.AioExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        [TestCase("s63", FakeBatchValue.S63BusinessUnit)]
        public async Task WhenInValidMessageQueueTrigger_ThenReturnsStandardAndAioExchangeSetIsNotCreated(string exchangeSetStandard, string businessUnit)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(true, true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).Returns(GetZipFiles(true, true));
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(false);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath)).Returns(true);

            var result = await fulfilmentDataService.CreateExchangeSet(message, FakeBatchValue.CurrentUtcDate);

            Assert.That(result, Is.EqualTo("Exchange Set Is Not Created"));
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardExchangeSet(message, salesCatalogueProductResponse, A<List<Products>>.Ignored, FakeBatchValue.ExchangeSetPath, salesCatalogueDataResponse, businessUnit)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetSubDirectories(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.ExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetZipFiles(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
        }

        [Test]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsLargeMediaAndAioExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).Returns(true);
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(false, true, true));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.LargeExchangeSetZipFileName5)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await fulfilmentDataService.CreateLargeExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.LargeExchangeSetFolderNamePattern);

            Assert.That(result, Is.EqualTo("Large Media Exchange Set Created Successfully"));
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.LargeExchangeSetZipFileName5)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 2);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 2);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test]
        [TestCase("s63")]
        public void WhenSerialAIOCreationFails_ThenReturnsLargeMediaAndAioExchangeSetIsNotCreated(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(true);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).Returns(true);
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).Returns(false);

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>().And.Message.EqualTo(FulfilmentExceptionMessage),
                async delegate { await fulfilmentDataService.CreateLargeExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.LargeExchangeSetFolderNamePattern); });

            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            fakeLogger.VerifyLogEntry(EventIds.AIOExchangeSetCreatedWithError, "AIO exchange creation failed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 0);
        }

        [Test, Description("Creating large media exchange set without AIO cell")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsLargeMediaEncExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponse(false);
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(false, false, true));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.LargeExchangeSetZipFileName5)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await fulfilmentDataService.CreateLargeExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.LargeExchangeSetFolderNamePattern);

            Assert.That(result, Is.EqualTo("Large Media Exchange Set Created Successfully"));
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, A<SalesCatalogueProductResponse>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.LargeExchangeSetMediaPath5, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.LargeExchangeSetZipFileName5)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.AioExchangeSetPath, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.AioExchangeSetZipFileName)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 1);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 1);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 1);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 1);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        [Test, Description("Creating only AIO exchange set")]
        [TestCase("s63")]
        public async Task WhenValidMessageQueueTrigger_ThenReturnsAIOExchangeSetCreatedSuccessfully(string exchangeSetStandard)
        {
            var salesCatalogueDataResponse = GetSalesCatalogueDataResponse();
            var message = GetScsResponseQueueMessage(exchangeSetStandard);
            var salesCatalogueProductResponse = GetSalesCatalogueProductResponseAioOnly();
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueDataResponse);
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(message, FakeBatchValue.HomeDirectoryPath, FakeBatchValue.CurrentUtcDate, A<LargeExchangeSetDataResponse>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern, FakeBatchValue.BatchPath)).Returns(true);
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).Returns(GetSubDirectories(false, true, false));
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).Returns(true);
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).Returns(true);

            var result = await fulfilmentDataService.CreateLargeExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.LargeExchangeSetFolderNamePattern);

            Assert.That(result, Is.EqualTo("Large Media Exchange Set Created Successfully"));
            A.CallTo(() => fakeFulfilmentSalesCatalogueService.GetSalesCatalogueDataResponse(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeExchangeSetBuilder.CreateStandardLargeMediaExchangeSet(A<SalesCatalogueServiceResponseQueueMessage>.Ignored, A<string>.Ignored, A<string>.Ignored, A<LargeExchangeSetDataResponse>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeExchangeSetBuilder.CreateAioExchangeSet(message, FakeBatchValue.CurrentUtcDate, FakeBatchValue.HomeDirectoryPath, A<List<Products>>.Ignored, A<SalesCatalogueDataResponse>.Ignored, salesCatalogueProductResponse)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(A<string>.Ignored, FakeBatchValue.LargeExchangeSetMediaPath5, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, FakeBatchValue.LargeExchangeSetZipFileName5)).MustNotHaveHappened();
            A.CallTo(() => fakeFulfilmentFileShareService.CreateZipFileForExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.AioExchangeSetPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.UploadZipFileForLargeMediaExchangeSetToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.AioExchangeSetZipFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFulfilmentFileShareService.CommitLargeMediaExchangeSet(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();

            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestStart, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 1);
            fakeLogger.VerifyLogEntry(EventIds.CreateZipFileRequestCompleted, "Create large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 1);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssStart, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", times: 1);
            fakeLogger.VerifyLogEntry(EventIds.UploadExchangeSetToFssCompleted, "Upload large media exchange set zip file request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 1);
            fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreated, "Large media exchange set is created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
        }

        #endregion
    }
}
