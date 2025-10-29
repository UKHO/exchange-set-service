using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentCleanUpService(ILogger<FulfilmentCleanUpService> logger, IFileSystemHelper fileSystemHelper, IOptions<CleanUpConfiguration> cleanUpConfiguration) : IFulfilmentCleanUpService
    {
        private readonly CleanUpConfiguration _cleanUpConfiguration = cleanUpConfiguration?.Value ?? throw new ArgumentNullException(nameof(cleanUpConfiguration));

        public void DeleteBatchFolder(FulfilmentServiceBatch batch)
        {
            if (_cleanUpConfiguration.Enabled)
            {
                var isDeleted = fileSystemHelper.DeleteFolderIfExists(batch.BatchDirectory);

                if (isDeleted)
                {
                    logger.LogInformation(EventIds.FulfilmentBatchTemporaryFolderDeleted.ToEventId(), "Temporary data folder deleted successfully for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", batch.BatchId, batch.CorrelationId);
                }
                else
                {
                    logger.LogError(EventIds.FulfilmentBatchTemporaryFolderNotFound.ToEventId(), "Temporary data folder not found for deletion for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", batch.BatchId, batch.CorrelationId);
                }
            }
        }

        public void DeleteHistoricBatchFolders(FulfilmentServiceBase fulfilmentServiceBase)
        {
            if (_cleanUpConfiguration.Enabled)
            {
                try
                {
                    var cutoffDate = fulfilmentServiceBase.CurrentUtcDateTime.AddDays(-_cleanUpConfiguration.NumberOfDays).Date;

                    foreach (var subFolder in fileSystemHelper.GetDirectories(fulfilmentServiceBase.BaseDirectory))
                    {
                        var subFolderName = new DirectoryInfo(subFolder).Name;
                        var folderIsValidDate = DateTimeExtensions.IsValidDate(subFolderName, FulfilmentServiceBase.CurrentUtcDateFormat, out var subFolderDateTime);

                        if (folderIsValidDate && subFolderDateTime.Date <= cutoffDate)
                        {
                            if (fileSystemHelper.DeleteFolderIfExists(subFolder))
                            {
                                // Even if folder is deleted successfully, log it as error. The FulfilmentServiceJob should have cleaned up after itself.
                                logger.LogError(EventIds.HistoricDateFolderDeleted.ToEventId(), "Historic folder deleted successfully for Date:{Date}.", subFolderName);
                            }
                            else
                            {
                                logger.LogError(EventIds.HistoricDateFolderNotFound.ToEventId(), "Historic folder not found for Date:{Date}.", subFolderName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.DeleteHistoricFoldersAndFilesException.ToEventId(), ex, "Exception while deleting historic folders and files with error {Message}", ex.Message);
                }
            }
        }
    }
}
