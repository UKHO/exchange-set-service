﻿using Azure;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
///using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
///using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string CONTENT_TYPE = "application/json";
        private const int StringLength = 2;

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
                        var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                        var cacheInfo = (FssSearchResponseCache)await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
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

            var processBatchDetail = async (BatchDetail internalBatchDetail, string downloadPath, string storageConnectionString) =>
            {
                foreach (var fileItem in internalBatchDetail.Files?.Select(a => a.Links.Get.Href))
                {
                    var uriArray = fileItem.Split("/");
                    var fileName = uriArray[^1];
                    fileSystemHelper.CheckAndCreateFolder(downloadPath);
                    string path = Path.Combine(downloadPath, fileName);
                    if (!fileSystemHelper.CheckFileExists(path))
                    {
                        BlockBlobClient cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, internalBatchDetail.BatchId);

                        //Added to check blob exception
                        try
                        {
                            await fileSystemHelper.DownloadToFileAsync(cloudBlockBlob, path);
                            updateNumbers.Add(itemUpdateNumber.Value);
                        }
                        catch (RequestFailedException storageEx) when (storageEx.ErrorCode == BlobErrorCode.BlobNotFound)
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
            };

            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId(), "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            var internalBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(cacheInfo.Response);
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId(), "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
            string downloadPath;

            if (CommonHelper.IsPeriodicOutputService)
            {
                var bundleLocation = item.Bundle.FirstOrDefault()?.Location.Split(";");
                exchangeSetRootPath = string.Format(exchangeSetRootPath, bundleLocation[0].Substring(1, 1), bundleLocation[1]);
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString());
            }
            else
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString(), itemUpdateNumber.Value.ToString());

            return logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceDownloadENCFilesFromCacheStart, EventIds.FileShareServiceDownloadENCFilesFromCacheCompleted,
                "File share service download request from cache container for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () => await processBatchDetail(internalBatchDetail,downloadPath,storageConnectionString)
                , item.ProductName, item.EditionNumber, itemUpdateNumber, internalBatchDetail.Files.Select(a => a.Links.Get.Href), queueMessage.BatchId, queueMessage.CorrelationId);
        }

        public async Task CopyFileToBlob(Stream stream, string fileName, string batchId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            BlockBlobClient cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(fileName, storageConnectionString, batchId);
            cloudBlockBlob.SetHttpHeaders(new BlobHttpHeaders { ContentType = CONTENT_TYPE });
            if (!await cloudBlockBlob.ExistsAsync())
            {
                await cloudBlockBlob.UploadAsync(stream);
            }
        }

        public async Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache)
        {
            //Temporary code to exclude storing of AIO cell in cahce table due to storage exception
            if (!fssSearchResponseCache.Response.Contains("GB800001"))
            {
                var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
            }
        }
    }
}
