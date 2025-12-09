using System;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public void DeleteHistoricBatchFolders(FulfilmentServiceBatchBase batchBase, DateTime currentUtcDateTime, CancellationToken cancellationToken)
        {
            try
            {
                if (fileSystemHelper.CheckDirectoryExists(batchBase.BaseDirectory))
                {
                    var cutoffDate = currentUtcDateTime.AddDays(-_cleanUpConfiguration.NumberOfDays);
                    var directories = fileSystemHelper.GetDirectoryInfo(batchBase.BaseDirectory);

                    foreach (var subFolder in directories)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (subFolder.CreationTime < cutoffDate)
                        {
                            if (fileSystemHelper.DeleteFolderIfExists(subFolder.FullName))
                            {
                                logger.LogError(EventIds.HistoricDateFolderDeleted.ToEventId(), "Historic folder deleted successfully for folder:{Folder}.", subFolder.Name);
                            }
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
