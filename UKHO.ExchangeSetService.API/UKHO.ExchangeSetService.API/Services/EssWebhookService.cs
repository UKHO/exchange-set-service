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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;


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
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IAuthFssTokenProvider authFssTokenProvider;
        private const string ServerHeaderValue = "Windows-Azure-Blob";

        public EssWebhookService(IAzureTableStorageClient azureTableStorageClient,
            ISalesCatalogueStorageService azureStorageService,
            IAzureBlobStorageClient azureBlobStorageClient,
            IEnterpriseEventCacheDataRequestValidator enterpriseEventCacheDataRequestValidator,
            IOptions<CacheConfiguration> cacheConfiguration,
            ILogger<EssWebhookService> logger,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig,
            IFileShareServiceCache fileShareServiceCache,
            IFileShareServiceClient fileShareServiceClient,
            IFileSystemHelper fileSystemHelper,
            IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
            IAuthFssTokenProvider authFssTokenProvider)
        {
            this.azureTableStorageClient = azureTableStorageClient;
            this.azureStorageService = azureStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.enterpriseEventCacheDataRequestValidator = enterpriseEventCacheDataRequestValidator;
            this.cacheConfiguration = cacheConfiguration;
            this.logger = logger;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
            this.fileShareServiceCache = fileShareServiceCache;
            this.fileShareServiceClient = fileShareServiceClient;
            this.fileSystemHelper = fileSystemHelper;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.authFssTokenProvider = authFssTokenProvider;
        }
        public Task<ValidationResult> ValidateEventGridCacheDataRequest(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest)
        {
            return enterpriseEventCacheDataRequestValidator.Validate(enterpriseEventCacheDataRequest);
        }

        public async Task InvalidateAndInsertCacheData(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest, string correlationId)
        {
            var productCode = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "ProductCode").Select(a => a.Value).FirstOrDefault();
            var cellName = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
            var editionNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
            var updateNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();

            if (ValidateCacheAttributeData(enterpriseEventCacheDataRequest.BusinessUnit, productCode, cellName, editionNumber, updateNumber))
            {
                var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(cacheConfiguration.Value.CacheStorageAccountName, cacheConfiguration.Value.CacheStorageAccountKey);

                var fssSearchResponse = new FssSearchResponseCache
                {
                    BatchId = enterpriseEventCacheDataRequest.BatchId,
                    PartitionKey = cellName,
                    RowKey = $"{editionNumber}|{updateNumber}|{enterpriseEventCacheDataRequest.BusinessUnit.ToUpper()}",
                    Response = JsonConvert.SerializeObject(enterpriseEventCacheDataRequest)
                };

                await DeleteSearchAndDownloadCacheData(fssSearchResponse, storageConnectionString, correlationId);
                if (enterpriseEventCacheDataRequest.Files.Count > 0 && enterpriseEventCacheDataRequest.Files != null)
                {
                    await CacheSearchAndDownloadData(fssSearchResponse, correlationId);
                }
                else
                {
                    logger.LogInformation(EventIds.CacheSearchAndDownloadInvalidData.ToEventId(), "Cache search and download files data missing in Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, productCode, correlationId);
                }
            }
            else
            {
                logger.LogInformation(EventIds.InvalidateAndInsertCacheInvalidDataFoundEvent.ToEventId(), "Invalid data found in search and download cache Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, productCode, correlationId);
            }
        }

        private async Task DeleteSearchAndDownloadCacheData(FssSearchResponseCache fssSearchResponse, string storageConnectionString, string correlationId)
        {
            var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(fssSearchResponse.PartitionKey, fssSearchResponse.RowKey, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
            string[] subsOfRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);

            logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId(), "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], correlationId);
            if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
            {
                var cacheTableData = new CacheTableData
                {
                    BatchId = cacheInfo.BatchId,
                    PartitionKey = cacheInfo.PartitionKey,
                    RowKey = cacheInfo.RowKey,
                    ETag = "*"
                };

                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId(), "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, subsOfRowKeys[2], cacheTableData.BatchId, correlationId);
                await azureTableStorageClient.DeleteAsync(cacheTableData, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString, essFulfilmentStorageconfig.Value.StorageContainerName);
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId(), "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, subsOfRowKeys[2], cacheTableData.BatchId, correlationId);

                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId(), "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], cacheTableData.BatchId, correlationId);
                await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, cacheTableData.BatchId);
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId(), "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], cacheTableData.BatchId, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId(), "No Matching Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} with ProductName:{cellName} and BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, subsOfRowKeys[2], correlationId);
            }
            logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId(), "Search and Download cache data deletion from table and Blob completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], correlationId);
        }

        private async Task CacheSearchAndDownloadData(FssSearchResponseCache fssSearchResponse, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var cacheBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(fssSearchResponse.Response);
            string[] subsOfRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);

            logger.LogInformation(EventIds.CacheSearchAndDownloadDataEventStart.ToEventId(), "Cache search and download data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], correlationId);

            foreach (var fileItem in cacheBatchDetail.Files?.Select(a => a.Links.Get.Href))
            {
                var fileName = fileItem.Split("/")[^1];
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, String.Empty, accessToken, fileItem, CancellationToken.None, correlationId);
                if (httpResponse.IsSuccessStatusCode)
                {
                    var requestUri = new Uri(httpResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);
                    var serverValue = httpResponse.Headers.Server.ToString().Split('/');
                    byte[] bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());

                    await fileShareServiceCache.CopyFileToBlob(new MemoryStream(bytes), fileName, fssSearchResponse.BatchId);
                    logger.LogInformation(EventIds.CacheSearchAndDownloadDataToBlobEvent.ToEventId(), "Cache search and download data, save file to blob for ProductName:{cellName} of BusinessUnit:{businessUnit} and FileName:{filename} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[2], fileName, correlationId);
                    if (serverValue[0] == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadENCFiles307RedirectResponse.ToEventId(), "Cache search and download data, download ENC file:{fileName} redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fileName, requestUri, fssSearchResponse.BatchId, correlationId);
                    }
                }
                else
                {
                    logger.LogError(EventIds.DownloadENCFilesNonOkResponse.ToEventId(), "Error in search and download cache data while downloading ENC file:{fileName} with uri:{uri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, fileItem, httpResponse.StatusCode, fssSearchResponse.BatchId, correlationId);
                }
            }
            var fssSearchResponseCache = new FssSearchResponseCache
            {
                BatchId = fssSearchResponse.BatchId,
                PartitionKey = fssSearchResponse.PartitionKey,
                RowKey = fssSearchResponse.RowKey,
                Response = JsonConvert.SerializeObject(cacheBatchDetail)
            };

            await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
            "File share service search response insert/merge request in azure table for cache for Product/CellName:{cellName}, EditionNumber:{editionNumber} and UpdateNumber:{updateNumber} with FSS BatchId:{cacheInfo.BatchId}",
            async () =>
            {
                await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                return Task.CompletedTask;
            }, fssSearchResponse.PartitionKey, subsOfRowKeys[0], subsOfRowKeys[1], fssSearchResponse.BatchId);

            logger.LogInformation(EventIds.CacheSearchAndDownloadDataCompleted.ToEventId(), "Cache search and download data to blob container and table completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{enterpriseEventCacheDataRequest.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, subsOfRowKeys[1], fssSearchResponse.BatchId, correlationId);
        }

        private bool ValidateCacheAttributeData(string businessUnit, string productCode, string cellName, string editionNumber, string updateNumber)
        {
            return ((string.Equals(businessUnit, cacheConfiguration.Value.S63CacheBusinessUnit, StringComparison.OrdinalIgnoreCase) || string.Equals(businessUnit, cacheConfiguration.Value.S57CacheBusinessUnit, StringComparison.OrdinalIgnoreCase))
                && productCode == cacheConfiguration.Value.CacheProductCode
                && !string.IsNullOrWhiteSpace(cellName)
                && !string.IsNullOrWhiteSpace(editionNumber)
                && !string.IsNullOrWhiteSpace(updateNumber));
        }
    }
}