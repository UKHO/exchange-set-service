using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentCleanUpService : IFulfilmentCleanUpService
    {
        private readonly ILogger<FulfilmentCleanUpService> _logger;
        private readonly string _storageAccountConnectionString;
        private readonly IAzureBlobStorageClient _azureBlobStorageClient;
        private readonly string _storageContainerName;
        private readonly IFileSystemHelper _fileSystemHelper;

        public FulfilmentCleanUpService(ILogger<FulfilmentCleanUpService> logger, ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageClient azureBlobStorageClient, IOptions<EssFulfilmentStorageConfiguration> storageConfiguration, IFileSystemHelper fileSystemHelper)
        {
            _logger = logger;
            _storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
            _azureBlobStorageClient = azureBlobStorageClient;
            var storageConfigurationValue = storageConfiguration?.Value ?? throw new ArgumentNullException(nameof(storageConfiguration));
            _storageContainerName = storageConfigurationValue.StorageContainerName;
            _fileSystemHelper = fileSystemHelper;
        }

        public async Task DeleteScsResponseAsync(FulfilmentServiceBatch batch)
        {
            var scsResponse = $"{batch.BatchId}.json";
            var blobClient = await _azureBlobStorageClient.GetBlobClient(scsResponse, _storageAccountConnectionString, _storageContainerName);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response)
            {
                _logger.LogInformation(EventIds.FulfilmentBatchScsResponseDeleted.ToEventId(), "SCS response json file {ScsResponseFileName} deleted successfully from the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", scsResponse, batch.BatchId, batch.CorrelationId);
            }
            else
            {
                _logger.LogError(EventIds.FulfilmentBatchScsResponseNotFound.ToEventId(), "SCS response json file {ScsResponseFileName} not found in the container for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", scsResponse, batch.BatchId, batch.CorrelationId);
            }
        }

        public void DeleteBatchFolder(FulfilmentServiceBatch batch)
        {
            var isDeleted = _fileSystemHelper.DeleteFolderIfExists(batch.BatchDirectory);

            if (isDeleted)
            {
                _logger.LogInformation(EventIds.FulfilmentBatchTemporaryFolderDeleted.ToEventId(), "Temporary data folder deleted successfully for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", batch.BatchId, batch.CorrelationId);
            }
            else
            {
                _logger.LogError(EventIds.FulfilmentBatchTemporaryFolderNotFound.ToEventId(), "Temporary data folder not found for deletion for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", batch.BatchId, batch.CorrelationId);
            }
        }
    }
}
