using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.API.Services
{
    public class EssWebhookService : IEssWebhookService
    {
        private readonly IAzureTableStorageClient azureTableStorageClient;
        private readonly ISalesCatalogueStorageService azureStorageService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IEnterpriseEventCacheDataRequestValidator enterpriseEventCacheDataRequestValidator;
        private readonly IOptions<CacheConfiguration> cacheConfiguration;
        private readonly ILogger<EssWebhookService> logger;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;

        public EssWebhookService(IAzureTableStorageClient azureTableStorageClient,
            ISalesCatalogueStorageService azureStorageService,
            IAzureBlobStorageClient azureBlobStorageClient,
            IEnterpriseEventCacheDataRequestValidator enterpriseEventCacheDataRequestValidator,
            IOptions<CacheConfiguration> cacheConfiguration,
            ILogger<EssWebhookService> logger,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            this.azureTableStorageClient = azureTableStorageClient;
            this.azureStorageService = azureStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.enterpriseEventCacheDataRequestValidator = enterpriseEventCacheDataRequestValidator;
            this.cacheConfiguration = cacheConfiguration;
            this.logger = logger;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
        }
        public Task<ValidationResult> ValidateEventGridCacheDataRequest(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest)
        {
            return enterpriseEventCacheDataRequestValidator.Validate(enterpriseEventCacheDataRequest);
        }

        public async Task DeleteSearchAndDownloadCacheData(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest, string correlationId)
        {
            var productCode = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "ProductCode").Select(a => a.Value).FirstOrDefault();
            var cellName = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
            var editionNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
            var updateNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();

            if (ValidateCacheAttributeData(enterpriseEventCacheDataRequest.BusinessUnit, productCode, cellName, editionNumber, updateNumber))
            {
                var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(cacheConfiguration.Value.CacheStorageAccountName, cacheConfiguration.Value.CacheStorageAccountKey);
                var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(cellName, editionNumber + "|" + updateNumber, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId(), "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} and _X-Correlation-ID:{CorrelationId}", cellName, correlationId);
                if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                {
                    var cacheTableData = new CacheTableData
                    {
                        BatchId = cacheInfo.BatchId,
                        PartitionKey = cacheInfo.PartitionKey,
                        RowKey = cacheInfo.RowKey,
                        ETag = "*"
                    };

                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId(), "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, cacheTableData.BatchId, correlationId);
                    await azureTableStorageClient.DeleteAsync(cacheTableData, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString, essFulfilmentStorageconfig.Value.StorageContainerName);
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId(), "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, cacheTableData.BatchId, correlationId);

                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId(), "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, cacheTableData.BatchId, correlationId);
                    await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, cacheTableData.BatchId);
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId(), "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, cacheTableData.BatchId, correlationId);
                }
                else
                {
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId(), "No Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} and ProductName:{cellName} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, correlationId);
                }
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId(), "Search and Download cache data from table and Blob completed for ProductName:{cellName} and _X-Correlation-ID:{CorrelationId}", cellName, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.DeleteSearchDownloadInvalidCacheDataFoundEvent.ToEventId(), "Invalid data found in Search and Download Cache Request for _X-Correlation-ID:{CorrelationId}", correlationId);
            }
        }

        private bool ValidateCacheAttributeData(string businessUnit, string productCode, string cellName, string editionNumber, string updateNumber)
        {
            return (businessUnit == cacheConfiguration.Value.CacheBusinessUnit && productCode == cacheConfiguration.Value.CacheProductCode && !string.IsNullOrWhiteSpace(cellName) && !string.IsNullOrWhiteSpace(editionNumber) && !string.IsNullOrWhiteSpace(updateNumber));
        }
    }
}