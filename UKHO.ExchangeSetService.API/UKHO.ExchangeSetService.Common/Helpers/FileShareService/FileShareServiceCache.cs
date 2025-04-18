﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Azure;
using Azure.Storage.Blobs.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.Common.Helpers;

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
        private const int StringLength = 2;
        private const int responseFileSizeLimitInKb = 60;

        public FileShareServiceCache(IAzureBlobStorageClient azureBlobStorageClient,
            IAzureTableStorageClient azureTableStorageClient,
            ILogger<FileShareServiceCache> logger,
            ISalesCatalogueStorageService azureStorageService,
            IOptions<CacheConfiguration> fssCacheConfiguration,
            IFileSystemHelper fileSystemHelper,
            IOptions<AioConfiguration> aioConfiguration)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.azureTableStorageClient = azureTableStorageClient;
            this.logger = logger;
            this.azureStorageService = azureStorageService;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
            this.aioConfiguration = aioConfiguration.Value;
        }

        public async Task<List<Products>> GetNonCachedProductDataForFss(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string businessUnit)
        {
            var suspectUpdateNumbersList = new List<int?>();
            var internalProductsNotFound = new List<Products>();

            foreach (var item in products)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(),
                        "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumbers:[{UpdateNumbers}]. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                        item.ProductName, item.EditionNumber, string.Join(",", item.UpdateNumbers.Select(a => a.Value)), queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }



                var updateNumbers = new List<int?>();
                foreach (var itemUpdateNumber in item.UpdateNumbers)
                {
                    updateNumbers.Clear();
                    var compareProducts = $"{item.ProductName}|{item.EditionNumber.Value}|{itemUpdateNumber.Value}|{businessUnit}";
                    var productList = new List<string>();

                    if (!productList.Contains(compareProducts))
                    {
                        var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                        var cacheInfo = await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value + "|" + businessUnit, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                        if (cacheInfo != null && string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            var blobClient = await azureBlobStorageClient.GetBlobClient($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);

                            if (blobClient != null)
                            {
                                cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync(blobClient);
                            }
                        }

                        if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, storageConnectionString, cacheInfo, businessUnit);

                            var fileCount = internalBatchDetail.Files.Count();

                            if (updateNumbers.Count == fileCount)
                            {
                                internalSearchBatchResponse.Entries.Add(internalBatchDetail);
                            }
                            else
                            {
                                suspectUpdateNumbersList.Add(itemUpdateNumber);
                            }
                            productList.Add(compareProducts);
                        }
                        else
                        {
                            suspectUpdateNumbersList.Add(itemUpdateNumber);
                        }
                    }
                }
                if (suspectUpdateNumbersList.Count > 0)
                {
                    internalProductsNotFound.Add(new Products
                    {
                        Cancellation = item.Cancellation,
                        Dates = item.Dates,
                        EditionNumber = item.EditionNumber,
                        FileSize = item.FileSize,
                        ProductName = item.ProductName,
                        UpdateNumbers = [.. suspectUpdateNumbersList],
                        Bundle = item.Bundle
                    });
                    suspectUpdateNumbersList.Clear();
                }
            }

            return internalProductsNotFound;
        }

        private Task<BatchDetail> CheckIfCacheProductsExistsInBlob(string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, Products item, List<int?> updateNumbers, int? itemUpdateNumber, string storageConnectionString, FssSearchResponseCache cacheInfo, string businessUnit)
        {
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId(), "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId);
            var internalBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(cacheInfo.Response);
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheCompleted.ToEventId(), "File share service search request from cache completed for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId);
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
                "File share service download request from cache container for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    foreach (var fileItem in internalBatchDetail.Files?.Select(a => a.Links.Get.Href))
                    {
                        var uriArray = fileItem.Split("/");
                        var fileName = uriArray[^1];
                        fileSystemHelper.CheckAndCreateFolder(downloadPath);
                        var path = Path.Combine(downloadPath, fileName);
                        if (!fileSystemHelper.CheckFileExists(path))
                        {
                            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, storageConnectionString, internalBatchDetail.BatchId);

                            //Added to check blob exception
                            try
                            {
                                await fileSystemHelper.DownloadToFileAsync(blobClient, path);
                                updateNumbers.Add(itemUpdateNumber.Value);
                            }
                            catch (RequestFailedException requestFailedException) when (requestFailedException.ErrorCode == BlobErrorCode.BlobNotFound.ToString())
                            {
                                logger.LogError(EventIds.GetBlobDetailsWithCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId, blobClient.Name, fileItem, requestFailedException.Message);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(EventIds.DownloadENCFilesFromCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId, blobClient.Name, fileItem, ex.Message);
                                throw new FulfilmentException(EventIds.DownloadENCFilesFromCacheContainerException.ToEventId());
                            }
                        }
                        internalBatchDetail.IgnoreCache = true;
                    }
                    return internalBatchDetail;
                }, item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, internalBatchDetail.Files.Select(a => a.Links.Get.Href), queueMessage.BatchId, queueMessage.CorrelationId);
        }

        //Returns path where fss files will get download for large media exchange set creation
        private string GetFileDownloadPath(string exchangeSetRootPath, Products item, int? itemUpdateNumber)
        {
            string downloadPath;
            var aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? [.. aioConfiguration.AioCells.Split(',')] : new List<string>();

            if (!aioCells.Contains(item.ProductName))
            {
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString());
            }
            else
            {
                downloadPath = Path.Combine(exchangeSetRootPath, item.ProductName.Substring(0, StringLength), item.ProductName, item.EditionNumber.Value.ToString(), itemUpdateNumber.Value.ToString());
            }
            return downloadPath;
        }

        public async Task CopyFileToBlob(Stream stream, string fileName, string batchId)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, storageConnectionString, batchId);

            if (!await blobClient.ExistsAsync())
            {
                await blobClient.UploadAsync(stream);
            }
        }

        public async Task InsertOrMergeFssCacheDetail(FssSearchResponseCache fssSearchResponseCache)
        {
            var responseSizeInKb = fssSearchResponseCache.Response.Length / 1024;

            //If content size is more that responseFileSizeLimitInKb
            //then store content into json file to avoid azure table storage exception for column limit
            if (responseSizeInKb > responseFileSizeLimitInKb)
            {
                // convert string to stream
                var byteArray = Encoding.ASCII.GetBytes(fssSearchResponseCache.Response);
                MemoryStream stream = new(byteArray);

                await CopyFileToBlob(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId);
                fssSearchResponseCache.Response = string.Empty;
            }

            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
        }

        public async Task<Stream> DownloadFileFromCacheAsync(string fileName, string containerName)
        {
            var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, storageConnectionString, containerName);

            var memoryStream = new MemoryStream();
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DownloadToAsync(memoryStream);
            }
            return memoryStream;
        }
    }
}
