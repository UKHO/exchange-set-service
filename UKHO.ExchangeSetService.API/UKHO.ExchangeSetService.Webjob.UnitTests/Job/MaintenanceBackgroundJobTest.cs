using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Job
{
    [TestFixture]
    public class MaintenanceBackgroundJobTest
    {
        private ILogger<MaintenanceBackgroundJob> _fakeLogger;
        private IFulfilmentCleanUpService _fakeFulfilmentCleanUpService;
        private CleanUpConfiguration _cleanUpConfiguration;
        private MaintenanceBackgroundJob _job;
        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<MaintenanceBackgroundJob>>();
            _fakeFulfilmentCleanUpService = A.Fake<IFulfilmentCleanUpService>();
            _cleanUpConfiguration = new CleanUpConfiguration
            {
                NumberOfDays = 2,
                MaintenanceCronSchedule = "0 0 4 * * *"
            };
            var fakeCleanUpConfiguration = A.Fake<IOptions<CleanUpConfiguration>>();
            A.CallTo(() => fakeCleanUpConfiguration.Value).Returns(_cleanUpConfiguration);
            _job = new MaintenanceBackgroundJob(FakeBatchValue.Configuration, _fakeLogger, _fakeFulfilmentCleanUpService, fakeCleanUpConfiguration);

            _cancellationTokenSource = new CancellationTokenSource();
            Environment.SetEnvironmentVariable("WEBSITE_SITE_NAME", "unittestsite");
        }

        [TearDown]
        public void TearDown()
        {
            _job.Dispose();
            _cancellationTokenSource.Dispose();
        }

        private static object InvokePrivate(object instance, string methodName, params object[] parameters)
        {
            var mi = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return mi!.Invoke(instance, parameters);
        }

        [Test]
        public async Task StartAsync_WhenCancelled_DoesNotThrowException()
        {
            _cancellationTokenSource.Cancel();

            Assert.DoesNotThrowAsync(async () => await _job.StartAsync(_cancellationTokenSource.Token));
        }

        [Test]
        public async Task ExecuteAsync_WhenCronMissing_LogsError()
        {
            _cleanUpConfiguration.MaintenanceCronSchedule = " ";
            var parameters = new object[] { _cancellationTokenSource.Token };

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error);
        }

        [Test]
        public async Task ExecuteAsync_WhenCancelled_DoNothing()
        {
            _cancellationTokenSource.Cancel();
            var parameters = new object[] { _cancellationTokenSource.Token };

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false, times: 0);
        }

        [Test]
        public async Task ExecuteAsync_ProcessAfterDelay()
        {
            var target = DateTime.UtcNow.AddSeconds(20);
            _cleanUpConfiguration.MaintenanceCronSchedule = $"{target.Second} {target.Minute} {target.Hour} * * *";
            var parameters = new object[] { _cancellationTokenSource.Token };

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
        }

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    if (!TryInitializeSchedule(out var initError))
        //    {
        //        logger.LogError(EventIds.MaintenanceCronScheduleInvalid.ToEventId(), "Maintenance background service disabled. Invalid cron expression. Error:{Error}", initError);
        //        return;
        //    }

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        var delay = _nextRunUtc - DateTime.UtcNow;

        //        if (delay < TimeSpan.Zero)
        //        {
        //            // If we're behind (e.g. cold start), run immediately.
        //            delay = TimeSpan.Zero;
        //        }

        //        try
        //        {
        //            await Task.Delay(delay, stoppingToken);
        //        }
        //        catch (TaskCanceledException)
        //        {
        //            break;
        //        }

        //        if (stoppingToken.IsCancellationRequested)
        //        {
        //            break;
        //        }

        //        RunMaintenance(DateTime.UtcNow);

        //        // Schedule next occurrence.
        //        _nextRunUtc = _schedule!.GetNextOccurrence(DateTime.UtcNow);
        //    }
        //}

        [Test]
        public void TryInitializeSchedule_WhenCronMissing_ReportsErrorAndReturnsFalse()
        {
            _cleanUpConfiguration.MaintenanceCronSchedule = " ";
            var parameters = new object[] { string.Empty };

            var result = (bool)InvokePrivate(_job, "TryInitializeSchedule", parameters);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(parameters[0], Is.EqualTo("Cron expression missing in configuration key CleanUpConfiguration:MaintenanceCronSchedule."));
            }

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false, times: 0);
        }

        [Test]
        public void TryInitializeSchedule_WhenCronInvalid_ReportsErrorAndReturnsFalse()
        {
            _cleanUpConfiguration.MaintenanceCronSchedule = "invalid";
            var parameters = new object[] { string.Empty };

            var result = (bool)InvokePrivate(_job, "TryInitializeSchedule", parameters);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.False);
                Assert.That(parameters[0].ToString(), Does.StartWith("Failed to parse cron expression"));
            }

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false, times: 0);
        }

        [Test]
        public void TryInitializeSchedule_WhenCronValid_LogsNextRunAndReturnsTrue()
        {
            var parameters = new object[] { string.Empty };

            var result = (bool)InvokePrivate(_job, "TryInitializeSchedule", parameters);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.True);
                Assert.That(parameters[0], Is.EqualTo(string.Empty));
            }

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false);
        }

        [Test]
        public void RunMaintenance_WhenSuccessful_LogsStartAndCompletedAndInvokesCleanUp()
        {
            var utcNow = DateTime.UtcNow;
            var parameters = new object[] { utcNow };

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).DoesNothing();

            InvokePrivate(_job, "RunMaintenance", parameters);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
        }

        [Test]
        public void RunMaintenance_WhenExceptionThrown_LogsFailure()
        {
            var utcNow = DateTime.UtcNow;
            var parameters = new object[] { utcNow };

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).Throws(new Exception("boom"));

            InvokePrivate(_job, "RunMaintenance", parameters);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBatchBase>.Ignored, utcNow)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesFailed, "Exception during maintenance (per-instance) - next run:{NextSchedule}. Exception:{Message}", logLevel: LogLevel.Error, checkIds: false);
        }
    }
}
