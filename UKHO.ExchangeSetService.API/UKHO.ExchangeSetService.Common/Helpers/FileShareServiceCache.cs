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
        private readonly IOptions<Storage1CacheConfiguration> fssCacheConfiguration1;
        private readonly IOptions<Storage2CacheConfiguration> fssCacheConfiguration2;

        public FileShareServiceCache(IAzureBlobStorageClient azureBlobStorageClient,
            IAzureTableStorageClient azureTableStorageClient,
            ILogger<FileShareServiceCache> logger,
            ISalesCatalogueStorageService azureStorageService,
            IOptions<CacheConfiguration> fssCacheConfiguration,
            IFileSystemHelper fileSystemHelper,
            IOptions<AioConfiguration> aioConfiguration,
            IOptions<Storage1CacheConfiguration> fssCacheConfiguration1,
            IOptions<Storage2CacheConfiguration> fssCacheConfiguration2)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.azureTableStorageClient = azureTableStorageClient;
            this.logger = logger;
            this.azureStorageService = azureStorageService;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
            this.aioConfiguration = aioConfiguration.Value;
            this.fssCacheConfiguration1 = fssCacheConfiguration1;
            this.fssCacheConfiguration2 = fssCacheConfiguration2;
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
                        //test code start====
                        var storageConnectionString1 = "";
                        Regex prodtobeinstorage1 = new Regex("[A-K]");
                        Regex prodtobeinstorage2 = new Regex("[K-Z]");
                        if (prodtobeinstorage1.IsMatch(compareProducts.Substring(0, 1)))
                        {
                            storageConnectionString1 = azureStorageService.GetStorageAccountConnectionString1(fssCacheConfiguration1.Value.CacheStorage1AccountName, fssCacheConfiguration1.Value.CacheStorage1AccountKey);
                            var cacheInfo1 = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString1);

                            if (cacheInfo1 != null && string.IsNullOrEmpty(cacheInfo1.Response))
                            {
                                CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob($"{cacheInfo1.BatchId}.json", storageConnectionString1, cacheInfo1.BatchId);

                                if (cloudBlockBlob != null)
                                {
                                    cacheInfo1.Response = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
                                }
                            }
                            if (cacheInfo1 != null && !string.IsNullOrEmpty(cacheInfo1.Response))
                            {
                                var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString1, cacheInfo1);

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
                        else
                        {
                            storageConnectionString1 = azureStorageService.GetStorageAccountConnectionString2(fssCacheConfiguration2.Value.CacheStorage2AccountName, fssCacheConfiguration2.Value.CacheStorage2AccountKey);
                            var cacheInfo2 = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration2.Value.FssSearchCache2TableName, storageConnectionString1);

                            if (cacheInfo2 != null && string.IsNullOrEmpty(cacheInfo2.Response))
                            {
                                CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob($"{cacheInfo2.BatchId}.json", storageConnectionString1, cacheInfo2.BatchId);

                                if (cloudBlockBlob != null)
                                {
                                    cacheInfo2.Response = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
                                }
                            }
                            if (cacheInfo2 != null && !string.IsNullOrEmpty(cacheInfo2.Response))
                            {
                                var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString1, cacheInfo2);

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

                        //test code end==== and commented og code below

                        //var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                        //var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                        //if (cacheInfo != null && string.IsNullOrEmpty(cacheInfo.Response))
                        //{
                        //    CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);

                        //    if (cloudBlockBlob != null)
                        //    {
                        //        cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
                        //    }
                        //}

                        //if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                        //{
                        //    var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString, cacheInfo);

                        //    int fileCount = internalBatchDetail.Files.Count();

                        //    if (updateNumbers.Count == fileCount)
                        //    {
                        //        internalSearchBatchResponse.Entries.Add(internalBatchDetail);
                        //    }
                        //    else
                        //    {
                        //        internalProductItemNotFound.UpdateNumbers.Add(itemUpdateNumber);
                        //    }
                        //    productList.Add(compareProducts);
                        //}
                        //else
                        //{
                        //    internalProductItemNotFound.UpdateNumbers.Add(itemUpdateNumber);
                        //}
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

        public async Task CopyFileToBlob1(Stream stream, string fileName, string batchId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString1(fssCacheConfiguration1.Value.CacheStorage1AccountName, fssCacheConfiguration1.Value.CacheStorage1AccountKey);
            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;
            if (!await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.UploadFromStreamAsync(stream);
            }
        }

        public async Task CopyFileToBlob2(Stream stream, string fileName, string batchId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString2(fssCacheConfiguration2.Value.CacheStorage2AccountName, fssCacheConfiguration2.Value.CacheStorage2AccountKey);
            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;
            if (!await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.UploadFromStreamAsync(stream);
            }
        }

        public async Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache)
        {
            int responseSizeInKb = fssSearchResponseCache.Response.Length / 1024;

            ////If content size is more that responseFileSizeLimitInKb
            ////then store content into json file to avoid azure table storage exception for column limit
            //if (responseSizeInKb > responseFileSizeLimitInKb)
            //{
            //    // convert string to stream
            //    byte[] byteArray = Encoding.ASCII.GetBytes(fssSearchResponseCache.Response);
            //    MemoryStream stream = new(byteArray);

            //    await CopyFileToBlob(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId);
            //    fssSearchResponseCache.Response = string.Empty;
            //}

            //var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            //await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

            //test code start==== and commented above og code

            Regex prodtobeinstorage1 = new Regex("[A-K]");
            //Regex prodtobeinstorage2 = new Regex("[K-Z]");
            var productList = fssSearchResponseCache.PartitionKey.Split(',');
            foreach (var product in productList)
            {
                if (prodtobeinstorage1.IsMatch(product.Substring(0, 1)))
                {
                    var storage1ConnectionString = azureStorageService.GetStorageAccountConnectionString1(fssCacheConfiguration1.Value.CacheStorage1AccountName, fssCacheConfiguration1.Value.CacheStorage1AccountKey);

                    if (responseSizeInKb > responseFileSizeLimitInKb)
                    {
                        // convert string to stream
                        byte[] byteArray = Encoding.ASCII.GetBytes(fssSearchResponseCache.Response);
                        MemoryStream stream = new(byteArray);

                        await CopyFileToBlob1(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId);
                        fssSearchResponseCache.Response = string.Empty;
                    }                    
                    await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration1.Value.FssSearchCache1TableName, storage1ConnectionString);

                }
                else
                {
                    var storage2ConnectionString = azureStorageService.GetStorageAccountConnectionString2(fssCacheConfiguration2.Value.CacheStorage2AccountName, fssCacheConfiguration2.Value.CacheStorage2AccountKey);

                    if (responseSizeInKb > responseFileSizeLimitInKb)
                    {
                        // convert string to stream
                        byte[] byteArray = Encoding.ASCII.GetBytes(fssSearchResponseCache.Response);
                        MemoryStream stream = new(byteArray);

                        await CopyFileToBlob2(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId);
                        fssSearchResponseCache.Response = string.Empty;
                    }
                    await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration2.Value.FssSearchCache2TableName, storage2ConnectionString);

                }
            }

            //test code end====
        }
    }
}
