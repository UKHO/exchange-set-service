using System;
using System.Threading;
using NCrontab;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IMaintenanceBackgroundService
    {
        TimeSpan CalculateNextRunDelay(DateTime utcNow, DateTime nextRunUtc);
        (bool Error, string Message, CrontabSchedule Schedule) GetSchedule();
        void RunMaintenance(DateTime utcNow, CrontabSchedule schedule, CancellationToken cancellationToken);
    }
}
