using System;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    public class FulfilmentCleanUpJob(IConfiguration configuration, ILogger<FulfilmentCleanUpJob> logger, IFulfilmentCleanUpService fulfilmentCleanUpService)
    {
        /// <summary>
        /// This should run once a day as per the cron expression in app settings.
        /// </summary>
        /// <param name="timerInfo"></param>
        public void DailyMaintenance([TimerTrigger("%CleanUpConfiguration:DailyMaintenanceCronSchedule%")] TimerInfo timerInfo)
        {
            try
            {
                var transactionName = $"{Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}-fulfilment-maintenance";
                Agent.Tracer.CaptureTransaction(transactionName, ApiConstants.TypeRequest, () =>
                {
                    logger.LogStartEndAndElapsedTime(EventIds.DeleteHistoricFoldersAndFilesStarted, EventIds.DeleteHistoricFoldersAndFilesCompleted, "Clean up process of historic folders and files", () =>
                    {
                        var batchBase = new FulfilmentServiceBatchBase(configuration, DateTime.UtcNow);
                        fulfilmentCleanUpService.DeleteHistoricBatchFolders(batchBase);
                        return true;
                    });
                });
            }
            catch (Exception ex)
            {
                var nextSchedule = timerInfo?.Schedule?.GetNextOccurrence(DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm") ?? "Unknown";
                logger.LogError(EventIds.DeleteHistoricFoldersAndFilesFailed.ToEventId(), ex, "Exception during daily maintenance. Next run:{NextSchedule}. Exception:{Message}", nextSchedule, ex.Message);
                Agent.Tracer.CurrentTransaction?.CaptureException(ex);
            }
            finally
            {
                Agent.Tracer.CurrentTransaction?.End();
            }
        }
    }
}
