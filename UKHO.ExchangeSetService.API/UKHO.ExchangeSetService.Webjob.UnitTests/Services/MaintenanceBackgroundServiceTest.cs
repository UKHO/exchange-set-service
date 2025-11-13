using System;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Services
{
    [TestFixture]
    public class MaintenanceBackgroundServiceTest
    {
        private ILogger<MaintenanceBackgroundService> _fakeLogger;
        private CleanUpConfiguration _cleanUpConfiguration;
        private IFulfilmentCleanUpService _fakeFulfilmentCleanUpService;
        private MaintenanceBackgroundService _service;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<MaintenanceBackgroundService>>();
            _cleanUpConfiguration = new CleanUpConfiguration
            {
                NumberOfDays = 2,
                MaintenanceCronSchedule = "0 0 4 * * *"
            };
            var fakeCleanUpConfiguration = A.Fake<IOptions<CleanUpConfiguration>>();
            A.CallTo(() => fakeCleanUpConfiguration.Value).Returns(_cleanUpConfiguration);
            _fakeFulfilmentCleanUpService = A.Fake<IFulfilmentCleanUpService>();
            _service = new MaintenanceBackgroundService(FakeBatchValue.Configuration, _fakeLogger, fakeCleanUpConfiguration, _fakeFulfilmentCleanUpService);

            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "unittestsite");
        }

        [Test]
        public void GetSchedule_WhenCronMissing_ReportErrorAndReturnFalse()
        {
            _cleanUpConfiguration.MaintenanceCronSchedule = " ";

            var (error, message, schedule) = _service.GetSchedule();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(error, Is.True);
                Assert.That(message, Is.EqualTo("Cron expression missing in configuration key CleanUpConfiguration:MaintenanceCronSchedule."));
                Assert.That(schedule, Is.Null);
            }
        }

        [Test]
        public void GetSchedule_WhenCronInvalid_ReportErrorAndReturnFalse()
        {
            _cleanUpConfiguration.MaintenanceCronSchedule = "invalid";

            var (error, message, schedule) = _service.GetSchedule();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(error, Is.True);
                Assert.That(message, Does.StartWith("Failed to parse cron expression"));
                Assert.That(schedule, Is.Null);
            }
        }

        [Test]
        public void GetSchedule_WhenCronValid_ReturnTrue()
        {
            var (error, message, schedule) = _service.GetSchedule();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(error, Is.False);
                Assert.That(message, Is.Empty);
                Assert.That(schedule, Is.Not.Null);
            }
        }

        [Test]
        public void RunMaintenance_WhenSuccessful_LogsStartAndCompletedAndInvokesCleanUp()
        {
            var utcNow = DateTime.UtcNow;
            var schedule = CrontabSchedule.Parse(_cleanUpConfiguration.MaintenanceCronSchedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).DoesNothing();

            _service.RunMaintenance(utcNow, schedule);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
        }

        [Test]
        public void RunMaintenance_WhenExceptionThrown_LogsFailure()
        {
            var utcNow = DateTime.UtcNow;
            var schedule = CrontabSchedule.Parse(_cleanUpConfiguration.MaintenanceCronSchedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).Throws(new Exception("boom"));

            _service.RunMaintenance(utcNow, schedule);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesFailed, "Exception during maintenance (per-instance) - next run:{NextSchedule}. Exception:{Message}", logLevel: LogLevel.Error, checkIds: false);
        }
    }
}
