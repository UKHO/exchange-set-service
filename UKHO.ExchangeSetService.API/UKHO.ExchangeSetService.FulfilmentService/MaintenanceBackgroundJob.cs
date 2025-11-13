using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    public static class MaintenanceBackgroundJobCollectionExtensions
    {
        public static IServiceCollection AddMaintenancePerInstance(this IServiceCollection services)
        {
            services.AddHostedService<MaintenanceBackgroundJob>();
            return services;
        }
    }

    public sealed class MaintenanceBackgroundJob(IConfiguration configuration, ILogger<MaintenanceBackgroundJob> logger, IFulfilmentCleanUpService fulfilmentCleanUpService, IOptions<CleanUpConfiguration> cleanUpConfiguration) : BackgroundService
    {
        private readonly CleanUpConfiguration _cleanUpConfiguration = cleanUpConfiguration?.Value ?? throw new ArgumentNullException(nameof(cleanUpConfiguration));
        private CrontabSchedule _schedule;
        private DateTime _nextRunUtc;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!TryInitializeSchedule(out var initError))
            {
                logger.LogError(EventIds.MaintenanceCronScheduleInvalid.ToEventId(), "Maintenance background service disabled. Invalid cron expression. Error:{Error}", initError);
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = _nextRunUtc - DateTime.UtcNow;

                if (delay < TimeSpan.Zero)
                {
                    // If we're behind (e.g. cold start), run immediately.
                    delay = TimeSpan.Zero;
                }

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

                RunMaintenance(DateTime.UtcNow);

                // Schedule next occurrence.
                _nextRunUtc = _schedule!.GetNextOccurrence(DateTime.UtcNow);
                logger.LogInformation(EventIds.MaintenanceNextScheduledRun.ToEventId(), "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", _nextRunUtc);
            }
        }

        private bool TryInitializeSchedule(out string error)
        {
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(_cleanUpConfiguration.MaintenanceCronSchedule))
            {
                error = "Cron expression missing in configuration key CleanUpConfiguration:MaintenanceCronSchedule.";
                return false;
            }

            try
            {
                _schedule = CrontabSchedule.Parse(_cleanUpConfiguration.MaintenanceCronSchedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            }
            catch (Exception ex)
            {
                error = $"Failed to parse cron expression '{_cleanUpConfiguration.MaintenanceCronSchedule}'. {ex.Message}";
                return false;
            }

            _nextRunUtc = _schedule.GetNextOccurrence(DateTime.UtcNow);
            logger.LogInformation(EventIds.MaintenanceNextScheduledRun.ToEventId(), "Maintenance background service - next run at {NextRunUtc:dd/MM/yyyy HH:mm:ss} (UTC).", _nextRunUtc);
            return true;
        }

        private void RunMaintenance(DateTime utcNow)
        {
            var transactionName = $"{Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}-fulfilment-maintenance";
            var transaction = Agent.Tracer.StartTransaction(transactionName, ApiConstants.TypeRequest);

            try
            {
                logger.LogStartEndAndElapsedTime(
                    EventIds.DeleteHistoricFoldersAndFilesStarted,
                    EventIds.DeleteHistoricFoldersAndFilesCompleted,
                    "Per-instance clean up process of historic folders and files",
                    () =>
                    {
                        var batchBase = new FulfilmentServiceBatchBase(configuration);
                        fulfilmentCleanUpService.DeleteHistoricBatchFolders(batchBase, utcNow);
                        return true;
                    });
            }
            catch (Exception ex)
            {
                var nextSchedule = _schedule?.GetNextOccurrence(utcNow).ToString("dd/MM/yyyy HH:mm") ?? "Unknown";
                logger.LogError(EventIds.DeleteHistoricFoldersAndFilesFailed.ToEventId(), ex, "Exception during maintenance (per-instance) - next run:{NextSchedule}. Exception:{Message}", nextSchedule, ex.Message);
                transaction.CaptureException(ex);
            }
            finally
            {
                transaction.End();
            }
        }
    }
}
