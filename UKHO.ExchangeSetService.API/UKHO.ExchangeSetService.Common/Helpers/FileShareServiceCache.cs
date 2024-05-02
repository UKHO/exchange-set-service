using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareServiceCache : IFileShareServiceCache
    {
        private readonly IRedisCache redisCache;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IAzureTableStorageClient azureTableStorageClient;
        private readonly ILogger<FileShareServiceCache> logger;
        private readonly ISalesCatalogueStorageService azureStorageService;
        private readonly IOptions<CacheConfiguration> fssCacheConfiguration;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly AioConfiguration aioConfiguration;
        private const string CONTENT_TYPE = "application/json";
        private const int StringLength = 2;
        private const int responseFileSizeLimitInKb = 60;

        public FileShareServiceCache(IAzureBlobStorageClient azureBlobStorageClient,
            IAzureTableStorageClient azureTableStorageClient,
            ILogger<FileShareServiceCache> logger,
            ISalesCatalogueStorageService azureStorageService,
            IOptions<CacheConfiguration> fssCacheConfiguration,
            IFileSystemHelper fileSystemHelper,
            IOptions<AioConfiguration> aioConfiguration, IRedisCache redisCache)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.azureTableStorageClient = azureTableStorageClient;
            this.logger = logger;
            this.azureStorageService = azureStorageService;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
            this.aioConfiguration = aioConfiguration.Value;
            this.redisCache = redisCache;
        }

        public async Task<List<Products>> GetNonCachedProductDataForFss(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var internalProductsNotFound = new List<Products>();

            foreach (var item in products)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumbers:[{UpdateNumbers}]. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                        item.ProductName, item.EditionNumber, string.Join(",", item.UpdateNumbers.Select(a => a.Value.ToString())), queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }
                var internalProductItemNotFound = new Products
                {
                    Cancellation = item.Cancellation,
                    Dates = item.Dates,
                    EditionNumber = item.EditionNumber,
                    FileSize = item.FileSize,
                    ProductName = item.ProductName,
                    UpdateNumbers = new List<int?>(),
                    Bundle = item.Bundle
                };

                List<int?> updateNumbers = new List<int?>();
                foreach (var itemUpdateNumber in item.UpdateNumbers)
                {
                    updateNumbers.Clear();
                    var compareProducts = $"{item.ProductName}|{item.EditionNumber.Value}|{itemUpdateNumber.Value}";
                    var productList = new List<string>();

                    if (!productList.Contains(compareProducts))
                    {
                        var storageAccountWithKey = GetStorageAccountNameAndKeyBasedOnAgencyCode(item.ProductName);
                        var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
                        var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
                        redisCache.RemoveData(compareProducts);
                        ////var cacheInfo = redisCache.GetCacheData<FssSearchResponseCache>(compareProducts);


                        if (cacheInfo != null && string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);
                            
                            if (cloudBlockBlob != null)
                            {
                                cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
                            }
                        }

                        if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString, cacheInfo);

                            int fileCount = internalBatchDetail.Files.Count();

                            if (updateNumbers.Count == fileCount)
                            {
                                internalSearchBatchResponse.Entries.Add(internalBatchDetail);
                            }
                            else
                            {
                                internalProductItemNotFound.UpdateNumbers.Add(itemUpdateNumber);
                            }
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

        private Task<BatchDetail> CheckIfCacheProductsExistsInBlob(string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, Products item, List<int?> updateNumbers, int? itemUpdateNumber, string storageConnectionString, FssSearchResponseCache cacheInfo)
        {
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId(), "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            var internalBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(cacheInfo.Response);
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId(), "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            string downloadPath;

            if (CommonHelper.IsPeriodicOutputService)
            {
                var bundleLocation = item.Bundle.FirstOrDefault().Location.Split(";");
                exchangeSetRootPath = string.Format(exchangeSetRootPath, bundleLocation[0].Substring(1, 1), bundleLocation[1]);
                downloadPath = GetFileDownloadPath(exchangeSetRootPath, item, itemUpdateNumber);
            }
            else
            {
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString(), itemUpdateNumber.Value.ToString());
            }

            return logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceDownloadENCFilesFromCacheStart, EventIds.FileShareServiceDownloadENCFilesFromCacheCompleted,
                "File share service download request from cache container for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    foreach (var fileItem in internalBatchDetail.Files?.Select(a => a.Links.Get.Href))
                    {
                        var uriArray = fileItem.Split("/");
                        var fileName = uriArray[^1];
                        fileSystemHelper.CheckAndCreateFolder(downloadPath);
                        string path = Path.Combine(downloadPath, fileName);
                        if (!fileSystemHelper.CheckFileExists(path))
                        {
                            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, internalBatchDetail.BatchId);

                            //Added to check blob exception
                            try
                            {
                                await fileSystemHelper.DownloadToFileAsync(cloudBlockBlob, path);
                                updateNumbers.Add(itemUpdateNumber.Value);
                            }
                            catch (StorageException storageEx) when (storageEx.Message.Contains("The specified blob does not exist"))
                            {
                                logger.LogError(EventIds.GetBlobDetailsWithCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId, cloudBlockBlob.Name, fileItem, storageEx.Message);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(EventIds.DownloadENCFilesFromCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId, cloudBlockBlob.Name, fileItem, ex.Message);
                                throw new FulfilmentException(EventIds.DownloadENCFilesFromCacheContainerException.ToEventId());
                            }
                        }
                        internalBatchDetail.IgnoreCache = true;
                    }
                    return internalBatchDetail;
                }, item.ProductName, item.EditionNumber, itemUpdateNumber, internalBatchDetail.Files.Select(a => a.Links.Get.Href), queueMessage.BatchId, queueMessage.CorrelationId);
        }

        //Returns path where fss files will get download for large media exchange set creation
        private string GetFileDownloadPath(string exchangeSetRootPath, Products item, int? itemUpdateNumber)
        {
            string downloadPath;
            if (aioConfiguration.IsAioEnabled)
            {
                List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();

                if (!aioCells.Contains(item.ProductName))
                    downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString());
                else
                    downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString(), itemUpdateNumber.Value.ToString());
            }
            else
            {
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString());
            }

            return downloadPath;
        }

        public async Task CopyFileToBlob(Stream stream, string fileName, string batchId, string code)
        {
            var storageAccountWithKey = GetStorageAccountNameAndKeyBasedOnAgencyCode(code);
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;
            if (!await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.UploadFromStreamAsync(stream);
            }
        }

        private (string, string) GetStorageAccountNameAndKeyBasedOnAgencyCode(string code)
        {
            return code switch
            {
               string c when Regex.IsMatch(c, "^[a-mA-M]") => (fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey),
               string c when Regex.IsMatch(c, "^[n-zN-Z]") => (fssCacheConfiguration.Value.CacheStorageAccountName1, fssCacheConfiguration.Value.CacheStorageAccountKey1),
                  _ => (string.Empty, string.Empty),
            };
        }

        public async Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache)
        {
            int responseSizeInKb = fssSearchResponseCache.Response.Length / 1024;

            //If content size is more that responseFileSizeLimitInKb
            //then store content into json file to avoid azure table storage exception for column limit
            if (responseSizeInKb > responseFileSizeLimitInKb)
            {
                // convert string to stream
                byte[] byteArray = Encoding.ASCII.GetBytes(fssSearchResponseCache.Response);
                MemoryStream stream = new(byteArray);

                await CopyFileToBlob(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId, fssSearchResponseCache.PartitionKey);
                fssSearchResponseCache.Response = string.Empty;
            }
            var storageAccountWithKey = GetStorageAccountNameAndKeyBasedOnAgencyCode(fssSearchResponseCache.PartitionKey);
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
            await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

            //redisCache.SetCacheData<FssSearchResponseCache>($"{fssSearchResponseCache.PartitionKey}|{fssSearchResponseCache.RowKey}", fssSearchResponseCache);
        }
    }
}
