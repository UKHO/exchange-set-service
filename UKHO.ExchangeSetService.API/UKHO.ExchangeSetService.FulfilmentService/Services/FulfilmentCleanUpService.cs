using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentCleanUpService(ILogger<FulfilmentCleanUpService> logger, IFileSystemHelper fileSystemHelper) : IFulfilmentCleanUpService
    {
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
    }
}
