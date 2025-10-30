using System.IO;
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
            _cleanUpConfiguration = new CleanUpConfiguration
            {
                Enabled = true,
                NumberOfDays = 2
            };
            var fakeCleanUpConfiguration = A.Fake<IOptions<CleanUpConfiguration>>();
            A.CallTo(() => fakeCleanUpConfiguration.Value).Returns(_cleanUpConfiguration);
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();

            _service = new FulfilmentCleanUpService(
                _fakeLogger,
                _fakeFileSystemHelper,
                fakeCleanUpConfiguration);

            var message = new SalesCatalogueServiceResponseQueueMessage
            {
                BatchId = FakeBatchValue.BatchId,
                CorrelationId = FakeBatchValue.CorrelationId
            };

            _batch = new FulfilmentServiceBatch(FakeBatchValue.Configuration, message, FakeBatchValue.CurrentUtcDateTime);
        }

        [Test]
        public void DeleteBatchFolder_WhenCleanUpDisabled_DoesNothing()
        {
            _cleanUpConfiguration.Enabled = false;

            _service.DeleteBatchFolder(_batch);

            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(FakeBatchValue.BatchPath)).MustNotHaveHappened();
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderDeleted, "Temporary data folder deleted successfully for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.FulfilmentBatchTemporaryFolderNotFound, "Temporary data folder not found for deletion for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", logLevel: LogLevel.Error, times: 0);
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
        public void DeleteHistoricBatchFolders_WhenCleanUpDisabled_DoesNothing()
        {
            _cleanUpConfiguration.Enabled = false;

            _service.DeleteHistoricBatchFolders(_batch);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for Date:{Date}.", logLevel: LogLevel.Error, checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderNotFound, "Historic folder not found for Date:{Date}.", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesException, "Exception while deleting historic folders and files with error {Message}", logLevel: LogLevel.Error, checkIds: false, times: 0);
        }

        [Test]
        public void DeleteHistoricBatchFolders_WhenNoHistoricFoldersFound_DoesNothing()
        {
            var directories = new string[]
            {
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.AddDays(-1).ToString(FulfilmentServiceBase.CurrentUtcDateFormat)), // Within retention period
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.ToString(FulfilmentServiceBase.CurrentUtcDateFormat)),             // Within retention period
                Path.Combine(FakeBatchValue.BaseDirectoryPath, "SomeOtherFolder")                                                                                   // Non-date folder
            };
            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).Returns(directories);

            _service.DeleteHistoricBatchFolders(_batch);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for Date:{Date}.", logLevel: LogLevel.Error, checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderNotFound, "Historic folder not found for Date:{Date}.", checkIds: false, times: 1);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesException, "Exception while deleting historic folders and files with error {Message}", logLevel: LogLevel.Error, checkIds: false, times: 0);
        }

        [Test]
        public void DeleteHistoricBatchFolders_WhenHistoricFoldersDeleted_CallsDelete()
        {
            var directories = new string[]
            {
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.AddDays(-3).ToString(FulfilmentServiceBase.CurrentUtcDateFormat)), // Outside retention period
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.AddDays(-2).ToString(FulfilmentServiceBase.CurrentUtcDateFormat)), // Outside retention period
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.AddDays(-1).ToString(FulfilmentServiceBase.CurrentUtcDateFormat)), // Within retention period
                Path.Combine(FakeBatchValue.BaseDirectoryPath, FakeBatchValue.CurrentUtcDateTime.ToString(FulfilmentServiceBase.CurrentUtcDateFormat))              // Within retention period
            };
            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).Returns(directories);
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[0])).Returns(true);
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[1])).Returns(false);

            _service.DeleteHistoricBatchFolders(_batch);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[0])).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[1])).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[2])).MustNotHaveHappened();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(directories[3])).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for Date:{Date}.", logLevel: LogLevel.Error, checkIds: false, times: 1);
            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderNotFound, "Historic folder not found for Date:{Date}.", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesException, "Exception while deleting historic folders and files with error {Message}", logLevel: LogLevel.Error, checkIds: false, times: 0);
        }

        [Test]
        public void DeleteHistoricBatchFolders_WhenExceptionIsThrown_LogIt()
        {
            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).Throws(new IOException("Disk not accessible"));

            _service.DeleteHistoricBatchFolders(_batch);

            A.CallTo(() => _fakeFileSystemHelper.GetDirectories(FakeBatchValue.BaseDirectoryPath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.DeleteFolderIfExists(A<string>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderDeleted, "Historic folder deleted successfully for Date:{Date}.", logLevel: LogLevel.Error, checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.HistoricDateFolderNotFound, "Historic folder not found for Date:{Date}.", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesException, "Exception while deleting historic folders and files with error {Message}", logLevel: LogLevel.Error, checkIds: false, times: 1);
        }
    }
}
