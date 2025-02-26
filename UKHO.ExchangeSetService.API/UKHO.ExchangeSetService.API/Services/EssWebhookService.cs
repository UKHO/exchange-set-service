using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers.FileShare.FileShareInterfaces;


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
        private const string ReadMeContainerName = "readme";

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
            this.azureTableStorageClient = azureTableStorageClient ?? throw new ArgumentNullException(nameof(azureTableStorageClient));
            this.azureStorageService = azureStorageService ?? throw new ArgumentNullException(nameof(azureStorageService));
            this.azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            this.enterpriseEventCacheDataRequestValidator = enterpriseEventCacheDataRequestValidator ?? throw new ArgumentNullException(nameof(enterpriseEventCacheDataRequestValidator));
            this.cacheConfiguration = cacheConfiguration ?? throw new ArgumentNullException(nameof(cacheConfiguration));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig ?? throw new ArgumentNullException(nameof(essFulfilmentStorageconfig));
            this.fileShareServiceCache = fileShareServiceCache ?? throw new ArgumentNullException(nameof(fileShareServiceCache));
            this.fileShareServiceClient = fileShareServiceClient ?? throw new ArgumentNullException(nameof(fileShareServiceClient));
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.fileShareServiceConfig = fileShareServiceConfig ?? throw new ArgumentNullException(nameof(fileShareServiceConfig));
            this.authFssTokenProvider = authFssTokenProvider ?? throw new ArgumentNullException(nameof(authFssTokenProvider));
        }
        public Task<ValidationResult> ValidateEventGridCacheDataRequest(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest)
        {
            return enterpriseEventCacheDataRequestValidator.Validate(enterpriseEventCacheDataRequest);
        }

        public async Task InvalidateAndInsertCacheDataAsync(EnterpriseEventCacheDataRequest enterpriseEventCacheDataRequest, string correlationId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(cacheConfiguration.Value.CacheStorageAccountName, cacheConfiguration.Value.CacheStorageAccountKey);
            var readMeFileExist = enterpriseEventCacheDataRequest.Files?.Exists(x => x.Filename?.ToUpper() == fileShareServiceConfig.Value.ReadMeFileName);
            if (readMeFileExist == true)
            {
                var productType = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "Product Type").Select(a => a.Value).FirstOrDefault();
                await InvalidateReadMeFileCacheDataAsync(storageConnectionString, enterpriseEventCacheDataRequest.BusinessUnit, enterpriseEventCacheDataRequest.BatchId, productType, correlationId);
            }
            else
            {
                var productCode = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "ProductCode").Select(a => a.Value).FirstOrDefault();
                var cellName = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
                var editionNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
                var updateNumber = enterpriseEventCacheDataRequest.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();

                if (!ValidateCacheAttributeData(enterpriseEventCacheDataRequest.BusinessUnit, productCode, cellName, editionNumber, updateNumber))
                {
                    logger.LogInformation(EventIds.InsertCacheInvalidDataFoundEvent.ToEventId(), "Invalid data found in search and download cache Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, productCode, correlationId);
                }
                else
                {
                    var fssSearchResponse = new FssSearchResponseCache
                    {
                        BatchId = enterpriseEventCacheDataRequest.BatchId,
                        PartitionKey = cellName,
                        RowKey = $"{editionNumber}|{updateNumber}|{enterpriseEventCacheDataRequest.BusinessUnit.ToUpper()}",
                        Response = JsonConvert.SerializeObject(enterpriseEventCacheDataRequest)
                    };

                    await DeleteCacheDataAsync(fssSearchResponse, storageConnectionString, correlationId);
                    if (enterpriseEventCacheDataRequest.Files != null && enterpriseEventCacheDataRequest.Files.Count > 0)
                    {
                        await UploadDataToCacheAsync(fssSearchResponse, correlationId);
                    }
                    else
                    {
                        logger.LogInformation(EventIds.InsertCacheMissingData.ToEventId(), "Cache search and download files data missing in Request for ProductName:{cellName}, BusinessUnit:{businessUnit} and ProductCode:{productCode} and _X-Correlation-ID:{CorrelationId}", cellName, enterpriseEventCacheDataRequest.BusinessUnit, productCode, correlationId);
                    }
                }
            }
        }

        private async Task DeleteCacheDataAsync(FssSearchResponseCache fssSearchResponse, string storageConnectionString, string correlationId)
        {
            var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(fssSearchResponse.PartitionKey, fssSearchResponse.RowKey, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
            string[] cacheTableRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);

            logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventStart.ToEventId(), "Search and Download cache data deletion from table and Blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], correlationId);
            if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
            {
                var cacheTableData = new CacheTableData
                {
                    BatchId = cacheInfo.BatchId,
                    PartitionKey = cacheInfo.PartitionKey,
                    RowKey = cacheInfo.RowKey,
                    ETag = ETag.All
                };

                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableStarted.ToEventId(), "Deletion started for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheInfo.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, cacheTableRowKeys[2], cacheTableData.BatchId, correlationId);
                await azureTableStorageClient.DeleteAsync(cacheTableData, cacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString, essFulfilmentStorageconfig.Value.StorageContainerName);
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromTableCompleted.ToEventId(), "Deletion completed for Search and Download cache data from table:{cacheConfiguration.Value.FssSearchCacheTableName} for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, cacheTableRowKeys[2], cacheTableData.BatchId, correlationId);

                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId(), "Deletion started for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], cacheTableData.BatchId, correlationId);
                await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, cacheTableData.BatchId);
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId(), "Deletion completed for Search and Download cache data from Blob Container for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], cacheTableData.BatchId, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheNoDataFoundEvent.ToEventId(), "No Matching Product found in Search and Download Cache table:{cacheConfiguration.Value.FssSearchCacheTableName} with ProductName:{cellName} and BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", cacheConfiguration.Value.FssSearchCacheTableName, fssSearchResponse.PartitionKey, cacheTableRowKeys[2], correlationId);
            }
            logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataEventCompleted.ToEventId(), "Search and Download cache data deletion from table and Blob completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], correlationId);
        }

        private async Task UploadDataToCacheAsync(FssSearchResponseCache fssSearchResponse, string correlationId)
        {
            var cacheBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(fssSearchResponse.Response);
            string[] cacheTableRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);

            logger.LogInformation(EventIds.UploadCacheDataEventStart.ToEventId(), "Upload Cache data to table and blob started for ProductName:{cellName} of BusinessUnit:{businessUnit} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], correlationId);

            await UploadDataToCacheBlobAsync(cacheBatchDetail, fssSearchResponse, correlationId);

            await AddDataToCacheTableAsync(cacheBatchDetail, fssSearchResponse, correlationId);

            logger.LogInformation(EventIds.UploadCacheDataEventCompleted.ToEventId(), "Upload Cache data to blob container and table completed for ProductName:{cellName} of BusinessUnit:{businessUnit} and BatchId:{enterpriseEventCacheDataRequest.BatchId} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], fssSearchResponse.BatchId, correlationId);
        }

        private async Task UploadDataToCacheBlobAsync(BatchDetail cacheBatchDetail, FssSearchResponseCache fssSearchResponse, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string[] cacheTableRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);

            foreach (var fileItem in cacheBatchDetail.Files?.Select(a => a.Links.Get.Href))
            {
                var fileName = fileItem.Split("/")[^1];
                using var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, string.Empty, accessToken, fileItem, CancellationToken.None, correlationId);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var requestUri = new Uri(httpResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);
                    var serverValue = httpResponse.Headers.Server.ToString().Split('/');
                    byte[] bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());

                    await fileShareServiceCache.CopyFileToBlob(new MemoryStream(bytes), fileName, fssSearchResponse.BatchId);

                    logger.LogInformation(EventIds.UploadCacheDataToBlobEvent.ToEventId(), "Upload Cache data, save file to blob for ProductName:{cellName} of BusinessUnit:{businessUnit} and FileName:{filename} and _X-Correlation-ID:{CorrelationId}", fssSearchResponse.PartitionKey, cacheTableRowKeys[2], fileName, correlationId);
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
        }
        private async Task AddDataToCacheTableAsync(BatchDetail cacheBatchDetail, FssSearchResponseCache fssSearchResponse, string correlationId)
        {
            string[] cacheTableRowKeys = fssSearchResponse.RowKey.Split('|', StringSplitOptions.TrimEntries);
            var fssSearchResponseCache = new FssSearchResponseCache
            {
                BatchId = fssSearchResponse.BatchId,
                PartitionKey = fssSearchResponse.PartitionKey,
                RowKey = fssSearchResponse.RowKey,
                Response = JsonConvert.SerializeObject(cacheBatchDetail)
            };

            await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
            "File share service search response insert request in azure table for cache for Product/CellName:{cellName}, EditionNumber:{editionNumber} and UpdateNumber:{updateNumber} with FSS BatchId:{cacheInfo.BatchId}  and _X-Correlation-ID:{CorrelationId}",
            async () =>
            {
                await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                return Task.CompletedTask;
            }, fssSearchResponse.PartitionKey, cacheTableRowKeys[0], cacheTableRowKeys[1], cacheTableRowKeys[2], fssSearchResponse.BatchId, correlationId);

        }
        private bool ValidateCacheAttributeData(string businessUnit, string productCode, string cellName, string editionNumber, string updateNumber)
        {
            return ((string.Equals(businessUnit, cacheConfiguration.Value.S63CacheBusinessUnit, StringComparison.OrdinalIgnoreCase) || string.Equals(businessUnit, cacheConfiguration.Value.S57CacheBusinessUnit, StringComparison.OrdinalIgnoreCase))
                && productCode == cacheConfiguration.Value.CacheProductCode
                && !string.IsNullOrWhiteSpace(cellName)
                && !string.IsNullOrWhiteSpace(editionNumber)
                && !string.IsNullOrWhiteSpace(updateNumber));
        }
        private async Task InvalidateReadMeFileCacheDataAsync(string storageConnectionString, string businessUnit, string batchId, string productType, string correlationId)
        {
            if (!ValidateCacheAttributeDataForReadMeFile(businessUnit, productType))
            {
                logger.LogInformation(EventIds.InsertCacheInvalidDataFoundEvent.ToEventId(), "Invalid data found in search and download cache Request for readme.txt BusinessUnit:{businessUnit} and ProductCode:{productType} and _X-Correlation-ID:{CorrelationId}", businessUnit, productType, correlationId);
            }
            else
            {
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerStarted.ToEventId(), "Deletion started for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", businessUnit, batchId, correlationId);
                await azureBlobStorageClient.DeleteCacheContainer(storageConnectionString, ReadMeContainerName);
                logger.LogInformation(EventIds.DeleteSearchDownloadCacheDataFromContainerCompleted.ToEventId(), "Deletion completed for readme.txt file cache data from Blob Container for BusinessUnit:{businessUnit} and BatchId:{cacheTableData.BatchId} and _X-Correlation-ID:{CorrelationId}", businessUnit, batchId, correlationId);
            }
        }
        private bool ValidateCacheAttributeDataForReadMeFile(string businessUnit, string productType)
        {
            return ((string.Equals(businessUnit, cacheConfiguration.Value.S63CacheBusinessUnit, StringComparison.OrdinalIgnoreCase) || string.Equals(businessUnit, cacheConfiguration.Value.S57CacheBusinessUnit, StringComparison.OrdinalIgnoreCase))
                && productType == cacheConfiguration.Value.CacheProductCode);
        }
    }
}
