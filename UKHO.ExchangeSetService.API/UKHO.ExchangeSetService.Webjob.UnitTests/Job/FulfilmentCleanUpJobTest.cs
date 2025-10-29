using System;
using FakeItEasy;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Timers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.Job
{
    [TestFixture]
    public class FulfilmentCleanUpJobTest
    {
        private ILogger<FulfilmentCleanUpJob> _fakeLogger;
        private IFulfilmentCleanUpService _fakeFulfilmentCleanUpService;
        private FulfilmentCleanUpJob _service;

        [SetUp]
        public void SetUp()
        {
            _fakeLogger = A.Fake<ILogger<FulfilmentCleanUpJob>>();
            _fakeFulfilmentCleanUpService = A.Fake<IFulfilmentCleanUpService>();

            _service = new FulfilmentCleanUpJob(FakeBatchValue.Configuration, _fakeLogger, _fakeFulfilmentCleanUpService);
        }

        [Test]
        public void DailyMaintenance_Success_InvokesDeleteHistoricBatchFoldersAndLogsStartAndCompleted()
        {
            var timerInfo = new TimerInfo(null, new ScheduleStatus(), false);

            _service.DailyMaintenance(timerInfo);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBase>.Ignored)).MustHaveHappenedOnceExactly();

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Clean up process of historic folders and files", endEvent: true, checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesFailed, "Exception during daily maintenance. Next run:{NextSchedule}. Exception:{Message}", logLevel: LogLevel.Error, times: 0, checkIds: false);
        }

        [Test]
        public void DailyMaintenance_Failure_LogsErrorWithUnknownNextScheduleWhenNoScheduleProvided()
        {
            var timerInfo = new TimerInfo(null, new ScheduleStatus(), false);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBase>.Ignored)).Throws(new InvalidOperationException("boom"));

            _service.DailyMaintenance(timerInfo);

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesStarted, "Clean up process of historic folders and files", checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesCompleted, "Clean up process of historic folders and files", endEvent: true, checkIds: false);
            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesFailed, "Exception during daily maintenance. Next run:{NextSchedule}. Exception:{Message}", logLevel: LogLevel.Error, checkIds: false);
        }

        [Test]
        public void DailyMaintenance_Failure_LogsErrorWithFormattedNextSchedule()
        {
            var next = new DateTime(2030, 12, 25, 07, 30, 00, DateTimeKind.Utc);
            var timerInfo = new TimerInfo(new TestSchedule(next), new ScheduleStatus(), false);

            A.CallTo(() => _fakeFulfilmentCleanUpService.DeleteHistoricBatchFolders(A<FulfilmentServiceBase>.Ignored)).Throws(new Exception("failure"));

            _service.DailyMaintenance(timerInfo);

            _fakeLogger.VerifyLogEntry(EventIds.DeleteHistoricFoldersAndFilesFailed, "Exception during daily maintenance. Next run:{NextSchedule}. Exception:{Message}", logLevel: LogLevel.Error, checkIds: false);
        }

        private class TestSchedule(DateTime next) : TimerSchedule
        {
            private readonly DateTime _next = next;

            [Obsolete("This property is obsolete and will be removed in a future version. All TimerSchedule implementations should now handle their own DST transitions.")]
            public override bool AdjustForDST => throw new NotImplementedException();

            public override DateTime GetNextOccurrence(DateTime now) => _next;
        }
    }
}
