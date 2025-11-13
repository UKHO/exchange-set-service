using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using NCrontab;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.FulfilmentService;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Job
{
    [TestFixture]
    public class MaintenanceBackgroundJobTest
    {
        private ILogger<MaintenanceBackgroundJob> _fakeLogger;
        private IMaintenanceBackgroundService _fakeMaintenanceBackgroundService;
        private MaintenanceBackgroundJob _job;
        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<MaintenanceBackgroundJob>>();
            _fakeMaintenanceBackgroundService = A.Fake<IMaintenanceBackgroundService>();
            _job = new MaintenanceBackgroundJob(_fakeLogger, _fakeMaintenanceBackgroundService);

            _cancellationTokenSource = new CancellationTokenSource();
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

        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    var (error, message, schedule) = maintenanceBackgroundService.GetSchedule();

        //    if (error)
        //    {
        //        logger.LogError(EventIds.MaintenanceCronScheduleInvalid.ToEventId(), "Maintenance background service disabled. Invalid cron expression. Error:{Error}", message);
        //        return;
        //    }

        //    var nextRunUtc = ScheduleNextOccurrence(schedule);

        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        var delay = nextRunUtc - DateTime.UtcNow;

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

        //        maintenanceBackgroundService.RunMaintenance(DateTime.UtcNow, schedule);

        //        nextRunUtc = ScheduleNextOccurrence(schedule);
        //    }
        //}

        //private DateTime ScheduleNextOccurrence(CrontabSchedule schedule)
        //{
        //    var nextRunUtc = schedule.GetNextOccurrence(DateTime.UtcNow);
        //    logger.LogInformation(EventIds.MaintenanceNextScheduledRun.ToEventId(), "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", nextRunUtc);
        //    return nextRunUtc;
        //}

        [Test]
        public async Task ExecuteAsync_WhenGetScheduleReturnsError_LogErrorAndExit()
        {
            var parameters = new object[] { _cancellationTokenSource.Token };
            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).Returns((true, "Invalid cron expression", null));

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeMaintenanceBackgroundService.RunMaintenance(A<DateTime>.Ignored, A<CrontabSchedule>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error);
        }

        //[Test]
        //public async Task ExecuteAsync_WhenCancelled_DoNothing()
        //{
        //    _cancellationTokenSource.Cancel();
        //    var parameters = new object[] { _cancellationTokenSource.Token };

        //    var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
        //    await result;

        //    _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false);
        //    _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
        //    _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false, times: 0);
        //    _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false, times: 0);
        //}

        //[Test]
        //public async Task ExecuteAsync_ProcessAfterDelay()
        //{
        //    var target = DateTime.UtcNow.AddSeconds(20);
        //    _cleanUpConfiguration.MaintenanceCronSchedule = $"{target.Second} {target.Minute} {target.Hour} * * *";
        //    var parameters = new object[] { _cancellationTokenSource.Token };

        //    var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
        //    await result;

        //    _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", checkIds: false);
        //    _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
        //    _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Per-instance clean up process of historic folders and files", checkIds: false);
        //    _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Per-instance clean up process of historic folders and files", endEvent: true, checkIds: false);
        //}
    }
}
