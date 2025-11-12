using FakeItEasy;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentCleanUpServiceTest
    {
        private ILogger<FulfilmentCleanUpService> _fakeLogger;
        private IFileSystemHelper _fakeFileSystemHelper;
        private FulfilmentCleanUpService _service;
        private FulfilmentServiceBatch _batch;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentCleanUpService>>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            _service = new FulfilmentCleanUpService(
                _fakeLogger,
                _fakeFileSystemHelper);

            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId
            };

            _batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message, FakeBatchValue.CurrentUtcDateTime);
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
