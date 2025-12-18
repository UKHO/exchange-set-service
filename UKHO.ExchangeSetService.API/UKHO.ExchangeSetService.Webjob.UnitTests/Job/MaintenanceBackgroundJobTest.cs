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
        public void StartAsync_WhenCancelled_DoesNotThrowException()
        {
            _cancellationTokenSource.Cancel();
            var schedule = CrontabSchedule.Parse("0 0 1 * * *", new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).Returns((false, string.Empty, schedule));

            Assert.DoesNotThrowAsync(async () => await _job.StartAsync(_cancellationTokenSource.Token));
        }

        [Test]
        public async Task ExecuteAsync_WhenGetScheduleReturnsError_LogErrorAndExit()
        {
            var parameters = new object[] { _cancellationTokenSource.Token };
            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).Returns((true, "Invalid cron expression", null));

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeMaintenanceBackgroundService.CalculateNextRunDelay(A<DateTime>.Ignored, A<DateTime>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeMaintenanceBackgroundService.RunMaintenance(A<DateTime>.Ignored, A<CrontabSchedule>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc} (UTC).", checkIds: false, times: 0);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error);
        }

        [Test]
        public async Task ExecuteAsync_WhenCancelled_DoNothing()
        {
            _cancellationTokenSource.Cancel();
            var parameters = new object[] { _cancellationTokenSource.Token };
            var schedule = CrontabSchedule.Parse("0 0 1 * * *", new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).Returns((false, string.Empty, schedule));

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeMaintenanceBackgroundService.CalculateNextRunDelay(A<DateTime>.Ignored, A<DateTime>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => _fakeMaintenanceBackgroundService.RunMaintenance(A<DateTime>.Ignored, A<CrontabSchedule>.Ignored, A<CancellationToken>.Ignored)).MustNotHaveHappened();

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc} (UTC).", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
        }

        [Test]
        public async Task ExecuteAsync_ProcessAfterDelay_ThenCancel()
        {
            var delay = new TimeSpan(0, 0, 1); // 1 second delay
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(1750)); // Use a separate CTS to allow the task to complete
            var parameters = new object[] { cancellationTokenSource.Token };
            var schedule = CrontabSchedule.Parse("0 0 1 * * *", new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).Returns((false, string.Empty, schedule));
            A.CallTo(() => _fakeMaintenanceBackgroundService.CalculateNextRunDelay(A<DateTime>.Ignored, A<DateTime>.Ignored)).Returns(delay);

            var result = (Task)InvokePrivate(_job, "ExecuteAsync", parameters);
            await result;

            A.CallTo(() => _fakeMaintenanceBackgroundService.GetSchedule()).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeMaintenanceBackgroundService.CalculateNextRunDelay(A<DateTime>.Ignored, A<DateTime>.Ignored)).MustHaveHappenedTwiceExactly();
            A.CallTo(() => _fakeMaintenanceBackgroundService.RunMaintenance(A<DateTime>.Ignored, A<CrontabSchedule>.Ignored, cancellationTokenSource.Token)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceNextScheduledRun, "Maintenance background service - next run at {NextRunUtc} (UTC).", checkIds: false, times: 2);
            _fakeLogger.VerifyLogEntry(EventIds.MaintenanceCronScheduleInvalid, "Maintenance background service disabled. Invalid cron expression. Error:{Error}", checkIds: false, logLevel: LogLevel.Error, times: 0);
        }
    }
}
