using System;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class MaintenanceBackgroundService(IConfiguration configuration, ILogger<MaintenanceBackgroundService> logger, IOptions<CleanUpConfiguration> cleanUpConfiguration, IFulfilmentCleanUpService fulfilmentCleanUpService) : IMaintenanceBackgroundService
    {
        private readonly CleanUpConfiguration _cleanUpConfiguration = cleanUpConfiguration?.Value ?? throw new ArgumentNullException(nameof(cleanUpConfiguration));

        public (bool Error, string Message, CrontabSchedule Schedule) GetSchedule()
        {
            CrontabSchedule schedule;

            if (string.IsNullOrWhiteSpace(_cleanUpConfiguration.MaintenanceCronSchedule))
            {
                return (true, "Cron expression missing in configuration key CleanUpConfiguration:MaintenanceCronSchedule.", null);
            }

            try
            {
                schedule = CrontabSchedule.Parse(_cleanUpConfiguration.MaintenanceCronSchedule, new CrontabSchedule.ParseOptions { IncludingSeconds = true });
            }
            catch (Exception ex)
            {
                return (true, $"Failed to parse cron expression '{_cleanUpConfiguration.MaintenanceCronSchedule}'. {ex.Message}", null);
            }

            return (false, string.Empty, schedule);
        }

        public void RunMaintenance(DateTime utcNow, CrontabSchedule schedule)
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
                var nextSchedule = schedule?.GetNextOccurrence(utcNow).ToString("dd/MM/yyyy HH:mm") ?? "Unknown";
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
