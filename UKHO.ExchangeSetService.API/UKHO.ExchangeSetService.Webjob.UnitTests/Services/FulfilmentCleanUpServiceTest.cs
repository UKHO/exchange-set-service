using System;
using System.IO.Abstractions;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentCleanUpServiceTest
    {
        private ILogger<FulfilmentCleanUpService> _fakeLogger;
        private IFileSystemHelper _fakeFileSystemHelper;
        private CleanUpConfiguration _cleanUpConfiguration;
        private FulfilmentCleanUpService _service;
        private FulfilmentServiceBatch _batch;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentCleanUpService>>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _cleanUpConfiguration = new CleanUpConfiguration
            {
                NumberOfDays = 2,
                MaintenanceCronSchedule = "0 0 4 * * *"
            };
            var fakeCleanUpConfiguration = A.Fake<IOptions<CleanUpConfiguration>>();
            A.CallTo(() => fakeCleanUpConfiguration.Value).Returns(_cleanUpConfiguration);

            _service = new FulfilmentCleanUpService(
                _fakeLogger,
                _fakeFileSystemHelper,
                fakeCleanUpConfiguration);

            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId
            };

            _batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message);
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

        [Test]
        public void DeleteHistoricBatchFolders_WhenHistoricFolderDeleted_LogsSuccess()
        {
            var currentUtcDateTime = DateTime.UtcNow;
            var cutoffDate = currentUtcDateTime.AddDays(-_cleanUpConfiguration.NumberOfDays);

            var oldDir = A.Fake<IDirectoryInfo>();
            A.CallTo(() => oldDir.CreationTime).Returns(cutoffDate.AddTicks(-1)); // older than cutoff
            A.CallTo(() => oldDir.FullName).Returns($@"{FakeBatchValue.BaseDirectoryPath}\old-folder");
            A.CallTo(() => oldDir.Name).Returns("old-folder");

            var recentDir = A.Fake<IDirectoryInfo>();
            A.CallTo(() => recentDir.CreationTime).Returns(cutoffDate); // same as cutoff
            A.CallTo(() => recentDir.FullName).Returns($@"{FakeBatchValue.BaseDirectoryPath}\recent-folder");
            A.CallTo(() => recentDir.Name).Returns("recent-folder");

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BaseDirectoryPath)).Returns([oldDir, recentDir]);
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(oldDir.FullName)).Returns(true);

            _service.DeleteHistoricBatchFolders(_batch, currentUtcDateTime);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BaseDirectoryPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(oldDir.FullName)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(recentDir.FullName)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for folder:{Folder}.", logLevel: LogLevel.Error, checkIds: false);
        }

        [Test]
        public void DeleteHistoricBatchFolders_WhenHistoricFolderDeleteReturnsFalse_DoesNotLogSuccess()
        {
            var currentUtcDateTime = DateTime.UtcNow;
            var cutoffDate = currentUtcDateTime.AddDays(-_cleanUpConfiguration.NumberOfDays);

            var oldDir = A.Fake<IDirectoryInfo>();
            A.CallTo(() => oldDir.CreationTime).Returns(cutoffDate.AddTicks(-1));
            A.CallTo(() => oldDir.FullName).Returns($@"{FakeBatchValue.BaseDirectoryPath}\old-folder");
            A.CallTo(() => oldDir.Name).Returns("old-folder");

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BaseDirectoryPath)).Returns([oldDir]);
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(oldDir.FullName)).Returns(false);

            _service.DeleteHistoricBatchFolders(_batch, currentUtcDateTime);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BaseDirectoryPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(oldDir.FullName)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for folder:{Folder}.", logLevel: LogLevel.Error, times: 0, checkIds: false);
        }

        [Test]
        public void DeleteHistoricBatchFolders_WhenExceptionThrown_LogsException()
        {
            var currentUtcDateTime = DateTime.UtcNow;

            A.CallTo(() => _fakeFileSystemHelper.GetDirectoryInfo(FakeBatchValue.BaseDirectoryPath)).Throws(new Exception("boom"));

            _service.DeleteHistoricBatchFolders(_batch, currentUtcDateTime);

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesException, "Exception while deleting historic folders and files with error {Message}", logLevel: LogLevel.Error, checkIds: false);
        }
    }
}
