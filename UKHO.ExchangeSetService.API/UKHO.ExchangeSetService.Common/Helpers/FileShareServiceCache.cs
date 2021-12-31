using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareServiceCache : IFileShareServiceCache
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageClient azureTableStorageClient;
        private readonly ILogger<FileShareServiceCache> logger;
        private readonly ISalesCatalogueStorageService azureStorageService;
        private readonly IOptions<CacheConfiguration> fssCacheConfiguration;
        private readonly IFileSystemHelper fileSystemHelper;
        private const string CONTENT_TYPE = "application/json";

        public FileShareServiceCache(IAzureBlobStorageClient azureBlobStorageClient,
            IAzureTableStorageClient azureTableStorageClient,
            ILogger<FileShareServiceCache> logger,
            ISalesCatalogueStorageService azureStorageService,
            IOptions<CacheConfiguration> fssCacheConfiguration,
            IFileSystemHelper fileSystemHelper)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.azureTableStorageClient = azureTableStorageClient;
            this.logger = logger;
            this.azureStorageService = azureStorageService;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
        }

        public async Task<List<Products>> GetNonCachedProductDataForFss(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var internalProductsNotFound = new List<Products>();
            Products internalProductItemNotFound;
            BatchDetail internalBatchDetail;
            List<string> productList = new List<string>();

            foreach (var item in products)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumbers:[{UpdateNumbers}]. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, string.Join(",", item?.UpdateNumbers.Select(a => a.Value.ToString())), queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }
                internalProductItemNotFound = new Products
                {
                    Cancellation = item.Cancellation,
                    Dates = item.Dates,
                    EditionNumber = item.EditionNumber,
                    FileSize = item.FileSize,
                    ProductName = item.ProductName
                };
                internalProductItemNotFound.UpdateNumbers = new List<int?>();
                List<int?> updateNumbers = new List<int?>();
                foreach (var itemUpdateNumber in item.UpdateNumbers)
                {
                    var compareProducts = $"{item.ProductName}|{item.EditionNumber.Value}|{itemUpdateNumber.Value}";
                    if (!productList.Contains(compareProducts))
                    {
                        var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                        var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
                        if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString, cacheInfo);
                            internalSearchBatchResponse.Entries.Add(internalBatchDetail);
                            productList.Add(compareProducts);
                        }
                        else
                        {
                            internalProductItemNotFound.UpdateNumbers.Add(itemUpdateNumber);
                        }
                    }
                }
                if (internalProductItemNotFound.UpdateNumbers != null && internalProductItemNotFound.UpdateNumbers.Any())
                {
                    internalProductsNotFound.Add(internalProductItemNotFound);
                }
            }

            return internalProductsNotFound;
        }

        private async Task<BatchDetail> CheckIfCacheProductsExistsInBlob(string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, Products item, List<int?> updateNumbers, int? itemUpdateNumber, string storageConnectionString, FssSearchResponseCache cacheInfo)
        {
            BatchDetail internalBatchDetail;
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId(), "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            internalBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(cacheInfo.Response);
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId(), "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            var downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, 2), item.ProductName, item.EditionNumber.Value.ToString(), itemUpdateNumber.Value.ToString());
            logger.LogInformation(EventIds.FileShareServiceDownloadENCFilesFromCacheStart.ToEventId(), "File share service download request from cache container started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, internalBatchDetail.Files.Select(a => a.Links.Get.Href), queueMessage.BatchId, queueMessage.CorrelationId);
            foreach (var fileItem in internalBatchDetail.Files?.Select(a => a.Links.Get.Href))
            {
                var uriArray = fileItem.Split("/");
                var fileName = uriArray[^1];
                fileSystemHelper.CheckAndCreateFolder(downloadPath);
                string path = Path.Combine(downloadPath, fileName);
                if (!File.Exists(path))
                {
                    CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, internalBatchDetail.BatchId);
                    await fileSystemHelper.DownloadToFileAsync(cloudBlockBlob, path);
                }
                updateNumbers.Add(itemUpdateNumber.Value);
                internalBatchDetail.IsCached = internalBatchDetail.IgnoreCache = true;
            }
            logger.LogInformation(EventIds.FileShareServiceDownloadENCFilesFromCacheCompleted.ToEventId(), "File share service download request from cache container completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, internalBatchDetail.Files.Select(a => a.Links.Get.Href), queueMessage.BatchId, queueMessage.CorrelationId);
            return internalBatchDetail;
        }

        public async Task CopyFileToBlob(Stream stream, string fileName, string batchId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;
            if (!await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.UploadFromStreamAsync(stream);
            }
        }

        public async Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
        }
    }
}
