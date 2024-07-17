using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Extensions;
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
        private readonly IFileShareServiceCache fileShareServiceCache;

        public EssWebhookService(IAzureTableStorageClient azureTableStorageClient,
            ISalesCatalogueStorageService azureStorageService,
            IAzureBlobStorageClient azureBlobStorageClient,
            IEnterpriseEventCacheDataRequestValidator enterpriseEventCacheDataRequestValidator,
            IOptions<CacheConfiguration> cacheConfiguration,
            ILogger<EssWebhookService> logger,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig,
            IFileShareServiceCache fileShareServiceCache)
        {
            this.azureTableStorageClient = azureTableStorageClient;
            this.azureStorageService = azureStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.enterpriseEventCacheDataRequestValidator = enterpriseEventCacheDataRequestValidator;
            this.cacheConfiguration = cacheConfiguration;
            this.logger = logger;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            this.fileShareServiceCache = fileShareServiceCache;
        }
        public Task<ValidationResult> ValidateEventGridCacheDataRequest(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest)
        {
            return enterpriseEventCacheDataRequestValidator.Validate(enterpriseEventCacheDataRequest);
        }

        public async Task UpsertSearchAndDownloadCacheData(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest, string correlationId)
        {
            var productCode = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "ProductCode").Select(a => a.Value).FirstOrDefault();
            var cellName = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
            var editionNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
            var updateNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();

            if (ValidateCacheAttributeData(enterpriseEventCacheDataRequest.BusinessUnit, productCode, cellName, editionNumber, updateNumber))
            {
                var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(cacheConfiguration.Value.CacheStorageAccountName, cacheConfiguration.Value.CacheStorageAccountKey);
                var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(cellName, editionNumber + "|" + updateNumber + "|" + enterpriseEventCacheDataRequest.BusinessUnit.ToUpper(), cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                logger.LogInformation(EventIds.UpsertSearchAndDownloadCacheDataEventStart.ToEventId(), "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, correlationId);
                if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                {
                    var cacheTableData = new CacheTableData
                    {
                        BatchId = cacheInfo.BatchId,
                        PartitionKey = cacheInfo.PartitionKey,
                        RowKey = cacheInfo.RowKey,
                        ETag = "*"
                    };

                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId(), "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, enterpriseEventCacheDataRequest.BusinessUnit, cacheTableData.BatchId, correlationId);
                    await azureTableStorageClient.DeleteAsync(cacheTableData, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString, essFulfilmentStorageconfig.Value.StorageContainerName);
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId(), "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, enterpriseEventCacheDataRequest.BusinessUnit, cacheTableData.BatchId, correlationId);

                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId(), "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, cacheTableData.BatchId, correlationId);
                    await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, cacheTableData.BatchId);
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId(), "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, cacheTableData.BatchId, correlationId);
                }
                else
                {
                    logger.LogInformation(EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId(), "No Matching Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} with ProductName:{cellName} and BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, cellName, enterpriseEventCacheDataRequest.BusinessUnit, correlationId);
                }

                var fssSearchResponseCache = new FssSearchResponseCache
                {
                    BatchId = enterpriseEventCacheDataRequest.BatchId,
                    PartitionKey = cellName,
                    RowKey = $"{editionNumber}|{updateNumber}",
                    Response = JsonConvert.SerializeObject(enterpriseEventCacheDataRequest)
                };

                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
                    "File share service search response insert/merge request in azure table for cache for Product/CellName:{cellName}, EditionNumber:{editionNumber} and UpdateNumber:{updateNumber} with FSS BatchId:{cacheInfo.BatchId}",
                    async () =>
                    {
                        await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                        return Task.CompletedTask;
                    }, cellName, editionNumber, updateNumber, enterpriseEventCacheDataRequest.BatchId);

                logger.LogInformation(EventIds.UpsertSearchAndDownloadCacheDataEventCompleted.ToEventId(), "Search and Download cache data deletion from table and Blob completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.UpsertSearchAndDownloadInvalidCacheDataFoundEvent.ToEventId(), "Invalid data found in Search and Download Cache Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, productCode, correlationId);
            }
        }

        private bool ValidateCacheAttributeData(string businessUnit, string productCode, string cellName, string editionNumber, string updateNumber)
        {
            return ((string.Equals(businessUnit, cacheConfiguration.Value.S63CacheBusinessUnit, StringComparison.OrdinalIgnoreCase) || string.Equals(businessUnit, cacheConfiguration.Value.S57CacheBusinessUnit, StringComparison.OrdinalIgnoreCase))
                && productCode == cacheConfiguration.Value.CacheProductCode
                && !string.IsNullOrWhiteSpace(cellName)
                && !string.IsNullOrWhiteSpace(editionNumber)
                && !string.IsNullOrWhiteSpace(updateNumber));
        }

        ////private async Task CacheFilesToBlob(fssSearchResponseCache)
        ////{

        ////}

    }
}