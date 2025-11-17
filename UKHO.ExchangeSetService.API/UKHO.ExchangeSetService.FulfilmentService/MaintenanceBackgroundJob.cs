using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    /// <summary>
    /// Represents a background service that periodically performs maintenance tasks, such as cleaning up historic
    /// fulfilment batch folders and files, according to a configurable cron schedule.
    /// </summary>
    /// <remarks>The maintenance schedule is determined by the cron expression specified in the clean-up
    /// configuration. If the cron expression is invalid or missing, the background service is disabled and an error is
    /// logged. Maintenance tasks are executed at the scheduled times and errors during execution are logged. This
    /// service is intended to run as a singleton within the application's background processing infrastructure.</remarks>
    /// <param name="logger"></param>
    /// <param name="maintenanceBackgroundService"></param>
    public sealed class MaintenanceBackgroundJob(ILogger<MaintenanceBackgroundJob> logger, IMaintenanceBackgroundService maintenanceBackgroundService) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var (error, message, schedule) = maintenanceBackgroundService.GetSchedule();

            if (error)
            {
                logger.LogError(EventIds.MaintenanceCronScheduleInvalid.ToEventId(), "Maintenance background service disabled. Invalid cron expression. Error:{Error}", message);
                return;
            }

            var nextRunUtc = ScheduleNextOccurrence(schedule);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = maintenanceBackgroundService.CalculateNextRunDelay(DateTime.UtcNow, nextRunUtc);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                maintenanceBackgroundService.RunMaintenance(DateTime.UtcNow, schedule);

                nextRunUtc = ScheduleNextOccurrence(schedule);
            }
        }

        private DateTime ScheduleNextOccurrence(CrontabSchedule schedule)
        {
            var nextRunUtc = schedule.GetNextOccurrence(DateTime.UtcNow);
            logger.LogInformation(EventIds.MaintenanceNextScheduledRun.ToEventId(), "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", nextRunUtc);
            return nextRunUtc;
        }
    }
}
