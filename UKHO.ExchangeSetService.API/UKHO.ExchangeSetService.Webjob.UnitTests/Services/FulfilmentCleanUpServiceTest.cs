using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentCleanUpServiceTest
    {
        private ILogger<FulfilmentCleanUpService> _fakeLogger;
        private ISalesCatalogueStorageService _fakeScsStorageService;
        private IAzureBlobStorageClient _fakeAzureBlobStorageClient;
        private IOptions<EssFulfilmentStorageConfiguration> _storageConfig;
        private IFileSystemHelper _fakeFileSystemHelper;
        private FulfilmentCleanUpService _service;
        private FulfilmentServiceBatch _batch;
        private const string StorageAccountConnectionString = "UseDevelopmentStorage=true";
        private const string StorageContainerName = "test-container";

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentCleanUpService>>();
            _fakeScsStorageService = A.Fake<ISalesCatalogueStorageService>();
            _fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            _storageConfig = Options.Create(new EssFulfilmentStorageConfiguration
            {
                StorageContainerName = StorageContainerName
            });

            A.CallTo(() => _fakeScsStorageService.GetStorageAccountConnectionString(A<string>.Ignored, A<string>.Ignored)).Returns(StorageAccountConnectionString);

            _service = new FulfilmentCleanUpService(
                _fakeLogger,
                _fakeScsStorageService,
                _fakeAzureBlobStorageClient,
                _storageConfig,
                _fakeFileSystemHelper);

            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId
            };

            _batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message, FakeBatchValue.CurrentUtcDateTime);
        }

        [Test]
        public async Task DeleteScsResponseAsync_WhenBlobDeleted_LogsSuccess()
        {
            var fakeBlobClient = A.Fake<BlobClient>();
            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient($"{FakeBatchValue.BatchId}.json", StorageAccountConnectionString, StorageContainerName)).Returns(Task.FromResult(fakeBlobClient));
            A.CallTo(() => fakeBlobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, A<CancellationToken>.Ignored)).Returns(Response.FromValue(true, default));

            await _service.DeleteScsResponseAsync(_batch);

            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient($"{FakeBatchValue.BatchId}.json", StorageAccountConnectionString, StorageContainerName)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchScsResponseDeleted, "SCS response json file {ScsResponseFileName} deleted successfully from the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.");
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchScsResponseNotFound, "SCS response json file {ScsResponseFileName} not found in the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", logLevel: LogLevel.Error, times: 0);
        }

        [Test]
        public async Task DeleteScsResponseAsync_WhenBlobNotDeleted_LogsError()
        {
            var fakeBlobClient = A.Fake<BlobClient>();
            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient($"{FakeBatchValue.BatchId}.json", StorageAccountConnectionString, StorageContainerName)).Returns(Task.FromResult(fakeBlobClient));
            A.CallTo(() => fakeBlobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, A<CancellationToken>.Ignored)).Returns(Response.FromValue(false, default));

            await _service.DeleteScsResponseAsync(_batch);

            A.CallTo(() => _fakeAzureBlobStorageClient.GetBlobClient($"{FakeBatchValue.BatchId}.json", StorageAccountConnectionString, StorageContainerName)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchScsResponseDeleted, "SCS response json file {ScsResponseFileName} deleted successfully from the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchScsResponseNotFound, "SCS response json file {ScsResponseFileName} not found in the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", logLevel: LogLevel.Error);
        }

        [Test]
        public void DeleteBatchFolder_WhenFolderDeleted_LogsSuccess()
        {
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(FakeBatchValue.BatchPath)).Returns(true);

            _service.DeleteBatchFolder(_batch);

            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderDeleted, "Temporary data folder deleted successfully for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.");
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderNotFound, "Temporary data folder not found for deletion for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", logLevel: LogLevel.Error, times: 0);
        }

        [Test]
        public void DeleteBatchFolder_WhenFolderNotFound_LogsError()
        {
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(FakeBatchValue.BatchPath)).Returns(false);

            _service.DeleteBatchFolder(_batch);

            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(FakeBatchValue.BatchPath)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderDeleted, "Temporary data folder deleted successfully for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderNotFound, "Temporary data folder not found for deletion for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", logLevel: LogLevel.Error);
        }
    }
}
