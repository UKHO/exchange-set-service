using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Job
{
    [TestFixture]
    public class FulfilmentServiceJobTest
    {
        private IConfiguration _configuration;
        private IFulfilmentDataService _fakeFulfilmentDataService;
        private ILogger<FulfilmentServiceJob> _fakeLogger;
        private IFileSystemHelper _fakeFileSystemHelper;
        private IFileShareBatchService _fakeFileShareBatchService;
        private IFileShareUploadService _fakeFileShareUploadService;
        private IAzureBlobStorageService _fakeAzureBlobStorageService;
        private IFulfilmentCallBackService _fakeFulfilmentCallBackService;
        private IOptions<PeriodicOutputServiceConfiguration> _periodicOutputServiceConfiguration;
        private FulfilmentServiceJob _fulfilmentServiceJob;
        private const int LargeMediaExchangeSetSizeInMB = 300;

        [SetUp]
        public void SetUp()
        {
            var inMemSettings = new Dictionary<string, string>
            {
                { "HOME", @"C:\HOME" }
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemSettings)
                .Build();

            _fakeFulfilmentDataService = A.Fake<IFulfilmentDataService>();
            _fakeLogger = A.Fake<ILogger<FulfilmentServiceJob>>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeFileShareBatchService = A.Fake<IFileShareBatchService>();
            _fakeFileShareUploadService = A.Fake<IFileShareUploadService>();
            _fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();
            _fakeFulfilmentCallBackService = A.Fake<IFulfilmentCallBackService>();

            _periodicOutputServiceConfiguration = Options.Create(new PeriodicOutputServiceConfiguration
            {
                LargeMediaExchangeSetSizeInMB = LargeMediaExchangeSetSizeInMB, // threshold
                LargeExchangeSetFolderName = FakeBatchValue.LargeExchangeSetFolderNamePattern
            });

            _fulfilmentServiceJob = new FulfilmentServiceJob(
                _configuration,
                _fakeFulfilmentDataService,
                _fakeLogger,
                _fakeFileSystemHelper,
                _fakeFileShareBatchService,
                _fakeFileShareUploadService,
                FakeBatchValue.FileShareServiceConfiguration,
                _fakeAzureBlobStorageService,
                _fakeFulfilmentCallBackService,
                _periodicOutputServiceConfiguration);
        }

        [TearDown]
        public void TearDown()
        {
            CommonHelper.IsPeriodicOutputService = false;
        }

        private static QueueMessage BuildQueueMessage(long fileSizeBytes, bool includeScsUri = false)
        {
            var payload = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                FileSize = fileSizeBytes,
                ScsResponseUri = includeScsUri ? "https://test/response.json" : null
            };
            var json = JsonSerializer.Serialize(payload);
            return QueuesModelFactory.QueueMessage(
                messageId: "id",
                popReceipt: "pr",
                body: new BinaryData(json),
                dequeueCount: 1,
                nextVisibleOn: DateTimeOffset.UtcNow,
                insertedOn: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffset.UtcNow.AddMinutes(5));
        }

        [Test]
        public async Task ProcessQueueMessage_FileSizeOnOrBelowThreshold_CallsCreateExchangeSet()
        {
            var qm = BuildQueueMessage(fileSizeBytes: LargeMediaExchangeSetSizeInMB * 1024 * 1024); // <= LargeMediaExchangeSetSizeInMB

            A.CallTo(() => _fakeFulfilmentDataService.CreateExchangeSet(A<FulfilmentServiceBatch>.That.Matches(m => m.BatchId == FakeBatchValue.BatchId))).Returns("ok");

            await _fulfilmentServiceJob.ProcessQueueMessage(qm);

            Assert.That(CommonHelper.IsPeriodicOutputService, Is.False);
            A.CallTo(() => _fakeFulfilmentDataService.CreateExchangeSet(A<FulfilmentServiceBatch>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentDataService.CreateLargeExchangeSet(A<FulfilmentServiceBatch>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.AIOToggleIsOn, "ESS Webjob : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateExchangeSetRequestStart, "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateExchangeSetRequestCompleted, "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task ProcessQueueMessage_FileSizeAboveThreshold_CallsCreateLargeExchangeSet()
        {
            var qm = BuildQueueMessage(fileSizeBytes: (LargeMediaExchangeSetSizeInMB * 1024 * 1024) + 1); // > LargeMediaExchangeSetSizeInMB

            A.CallTo(() => _fakeFulfilmentDataService.CreateLargeExchangeSet(A<FulfilmentServiceBatch>.That.Matches(m => m.BatchId == FakeBatchValue.BatchId), FakeBatchValue.LargeExchangeSetFolderNamePattern)).Returns("large");

            await _fulfilmentServiceJob.ProcessQueueMessage(qm);

            Assert.That(CommonHelper.IsPeriodicOutputService, Is.True);
            A.CallTo(() => _fakeFulfilmentDataService.CreateLargeExchangeSet(A<FulfilmentServiceBatch>.Ignored, FakeBatchValue.LargeExchangeSetFolderNamePattern)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentDataService.CreateExchangeSet(A<FulfilmentServiceBatch>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.AIOToggleIsOn, "ESS Webjob : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateLargeExchangeSetRequestStart, "Create Large Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateLargeExchangeSetRequestCompleted, "Create Large Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true);
        }

        [Test]
        public async Task ProcessQueueMessage_WhenFulfilmentThrowsFulfilmentException_ErrorFileFlowExecuted()
        {
            var qm = BuildQueueMessage(10 * 1024 * 1024, includeScsUri: true);
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFulfilmentDataService.CreateExchangeSet(A<FulfilmentServiceBatch>.Ignored)).Throws(new FulfilmentException(EventIds.SystemException.ToEventId()));
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.That.EndsWith(FakeBatchValue.ErrorFileName))).Returns(true);
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, A<string>.That.EndsWith(FakeBatchValue.BatchId), FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).Returns(true);
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, A<string>.That.EndsWith(FakeBatchValue.BatchId), FakeBatchValue.ErrorFileName)).Returns(true);
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            await _fulfilmentServiceJob.ProcessQueueMessage(qm);

            A.CallTo(() => _fakeFulfilmentDataService.CreateExchangeSet(A<FulfilmentServiceBatch>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(A<string>.That.EndsWith(FakeBatchValue.BatchId))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(A<string>.That.EndsWith(FakeBatchValue.ErrorFileName), A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(A<string>.That.EndsWith(FakeBatchValue.ErrorFileName))).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, A<string>.That.EndsWith(FakeBatchValue.BatchId), FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, A<string>.That.EndsWith(FakeBatchValue.BatchId), FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(A<string>.Ignored, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, A<SalesCatalogueServiceResponseQueueMessage>.That.Matches(m => m.BatchId == FakeBatchValue.BatchId))).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.AIOToggleIsOn, "ESS Webjob : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateExchangeSetRequestStart, "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}");
            _fakeLogger.VerifyLogEntry(EventIds.CreateExchangeSetRequestCompleted, "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", true, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtIsUploaded, "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
            _fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreatedWithError, "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task CreateAndUploadErrorFile_FileUploadedAndCommitted_LogsUploadAndCallsCallback()
        {
            const string errorText = "error text";
            const string scsResponseUri = "https://test/ess/response.json";
            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                ScsResponseUri = scsResponseUri
            };
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).Returns(true);
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).Returns(true);
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, FakeBatchValue.ErrorFileName)).Returns(true);
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            await _fulfilmentServiceJob.CreateAndUploadErrorFileToFileShareService(message, EventIds.SystemException.ToEventId(), errorText, FakeBatchValue.BatchPath);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ErrorFilePath, errorText)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, message)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtIsUploaded, "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
            _fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreatedWithError, "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task CreateAndUploadErrorFile_FileNotCreated_StillCallsCallback()
        {
            const string errorText = "error text";
            const string scsResponseUri = "https://test/ess/response.json";
            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                ScsResponseUri = scsResponseUri
            };
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).Returns(false);
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            await _fulfilmentServiceJob.CreateAndUploadErrorFileToFileShareService(message, EventIds.SystemException.ToEventId(), errorText, FakeBatchValue.BatchPath);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ErrorFilePath, errorText)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, message)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtIsUploaded, "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreatedWithError, "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtNotCreated, "Error while creating error.txt for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task CreateAndUploadErrorFile_FileUploadedButCommitFails_LogsNotUploaded()
        {
            const string errorText = "error text";
            const string scsResponseUri = "https://test/ess/response.json";
            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                ScsResponseUri = scsResponseUri
            };
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).Returns(true);
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).Returns(true);
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, FakeBatchValue.ErrorFileName)).Returns(false);
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            await _fulfilmentServiceJob.CreateAndUploadErrorFileToFileShareService(message, EventIds.SystemException.ToEventId(), errorText, FakeBatchValue.BatchPath);

            A.CallTo(() => _fakeFileSystemHelper.CheckAndCreateFolder(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileContent(FakeBatchValue.ErrorFilePath, errorText)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CheckFileExists(FakeBatchValue.ErrorFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareUploadService.UploadFileToFileShareService(FakeBatchValue.BatchId, FakeBatchValue.BatchPath, FakeBatchValue.CorrelationId, FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileShareBatchService.CommitBatchToFss(FakeBatchValue.BatchId, FakeBatchValue.CorrelationId, FakeBatchValue.BatchPath, FakeBatchValue.ErrorFileName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, message)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtIsUploaded, "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.ExchangeSetCreatedWithError, "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.ErrorTxtNotUploaded, "Error while uploading error.txt file to file share service for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", logLevel: LogLevel.Error);
        }

        [Test]
        public async Task SendErrorCallBackResponse_DownloadsResponseAndSendsCallback()
        {
            const string scsResponseUri = "https://test/ess/response.json";
            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId,
                ScsResponseUri = scsResponseUri
            };
            var salesCatalogueProductResponse = new SalesCatalogueProductResponse();

            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).Returns(salesCatalogueProductResponse);

            await _fulfilmentServiceJob.SendErrorCallBackResponse(message);

            A.CallTo(() => _fakeAzureBlobStorageService.DownloadSalesCatalogueResponse(scsResponseUri, FakeBatchValue.BatchId, FakeBatchValue.CorrelationId)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, message)).MustHaveHappenedOnceExactly();
        }
    }
}
