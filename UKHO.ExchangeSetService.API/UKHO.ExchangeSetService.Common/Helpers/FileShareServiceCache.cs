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
using Azure.Data.Tables;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.InteropServices;
using Azure.Storage.Blobs;

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
        private readonly string serviceConnectionString;
        private readonly string serviceTableName;
        private const string CONTENT_TYPE = "application/json";
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
            serviceConnectionString = this.azureStorageService.GetStorageAccountConnectionString(
                    fssCacheConfiguration.Value.CacheStorageAccountName,
                    fssCacheConfiguration.Value.CacheStorageAccountKey
                    );
            serviceTableName = this.fssCacheConfiguration.Value.FssSearchCacheTableName;
        }

        public async Task<List<Products>> GetNonCachedProductDataForFss(
                List<Products> products,
                SearchBatchResponse internalSearchBatchResponse,
                string exchangeSetRootPath,
                SalesCatalogueServiceResponseQueueMessage queueMessage,
                CancellationTokenSource cancellationTokenSource,
                CancellationToken cancellationToken,
                string businessUnit)
        {
            // rhz var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(
            //        fssCacheConfiguration.Value.CacheStorageAccountName,
            //        fssCacheConfiguration.Value.CacheStorageAccountKey
            //        );
            //var tableName = fssCacheConfiguration.Value.FssSearchCacheTableName;
            bool hasResponse;
            var existingFiles = new List<int?>();
            var internalProductsNotFound = new List<Products>();

            var subKeys = new { edition = 0, updateNumber = 1, businessUnit = 2 };


            foreach (var product in products)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumbers:[{UpdateNumbers}]. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                        product.ProductName, product.EditionNumber, string.Join(",", product.UpdateNumbers.Select(a => a.Value.ToString())), queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }

                var internalProductItemNotFound = new Products
                {
                    Cancellation = product.Cancellation,
                    Dates = product.Dates,
                    EditionNumber = product.EditionNumber,
                    FileSize = product.FileSize,
                    ProductName = product.ProductName,
                    UpdateNumbers = product.UpdateNumbers,
                    Bundle = product.Bundle
                };

                await foreach (var cacheInfo in await azureTableStorageClient.RetrieveUpdatesFromTableStorageAsync<FssSearchResponseCache>(
                        product.ProductName, product.EditionNumber.Value, serviceTableName, serviceConnectionString))
                {
                    if (cacheInfo.RowKey.Split('|')[subKeys.businessUnit] == businessUnit)
                    {
                        var cacheUpdateNumber = int.Parse(cacheInfo.RowKey.Split('|')[subKeys.updateNumber]);
                        existingFiles.Clear();

                        if (cancellationToken.IsCancellationRequested)
                        {
                            logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files from cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                                product.ProductName, product.EditionNumber, cacheUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);
                            throw new OperationCanceledException();
                        }

                        hasResponse = !string.IsNullOrEmpty(cacheInfo.Response);


                        if (!hasResponse) //Why would response be empty?
                        {
                            logger.LogInformation(EventIds.LogRequest.ToEventId(), "Empty Response for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                                product.ProductName, product.EditionNumber, cacheUpdateNumber, queueMessage.BatchId, queueMessage.CorrelationId);

                            // rhz var blobClient = await azureBlobStorageClient.GetBlobClient($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);

                            //if (blobClient != null)
                            //{
                            //    cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync(blobClient);
                            //    hasResponse = true;
                            //}
                            cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync($"{cacheInfo.BatchId}.json", serviceConnectionString, cacheInfo.BatchId);
                            hasResponse = !string.IsNullOrEmpty(cacheInfo.Response);
                        }

                        if (hasResponse)
                        {
                            var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(
                                    exchangeSetRootPath,
                                    queueMessage,
                                    product,
                                    existingFiles,
                                    cacheUpdateNumber,
                                    cacheInfo,
                                    businessUnit);


                            if (existingFiles.Count == internalBatchDetail.Files.Count())
                            {
                                internalSearchBatchResponse.Entries.Add(internalBatchDetail);
                                internalProductItemNotFound.UpdateNumbers.Remove(cacheUpdateNumber);
                            }
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

        public async Task<List<Products>> RhzX_GetNonCachedProductDataForFss(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string businessUnit)
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
                    var compareProducts = $"{item.ProductName}|{item.EditionNumber.Value}|{itemUpdateNumber.Value}|{businessUnit}";
                    var productList = new List<string>();

                    if (!productList.Contains(compareProducts))
                    {
                        var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
                        var cacheInfo = await azureTableStorageClient.RetrieveFromTableStorageAsync<FssSearchResponseCache>(item.ProductName, item.EditionNumber + "|" + itemUpdateNumber.Value + "|" + businessUnit, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);

                        if (cacheInfo != null && string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            //rhz var blobClient = await azureBlobStorageClient.GetBlobClient($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);

                            //if (blobClient != null)
                            //{
                            //    cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync(blobClient);
                            //}

                            cacheInfo.Response = await azureBlobStorageClient.DownloadTextAsync($"{cacheInfo.BatchId}.json", storageConnectionString, cacheInfo.BatchId);
                        }

                        if (cacheInfo != null && !string.IsNullOrEmpty(cacheInfo.Response))
                        {
                            var internalBatchDetail = await CheckIfCacheProductsExistsInBlob(exchangeSetRootPath, queueMessage, item, updateNumbers, itemUpdateNumber, cacheInfo, businessUnit);

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

        private Task<BatchDetail> CheckIfCacheProductsExistsInBlob(string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage queueMessage, Products item, List<int?> updateNumbers, int? itemUpdateNumber, FssSearchResponseCache cacheInfo, string businessUnit)
        {
            logger.LogInformation(EventIds.FileShareServiceSearchENCFilesFromCacheStart.ToEventId(), "File share service search request from cache started for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId);
            //rhz removed var internalBatchDetail = JsonConvert.DeserializeObject<BatchDetail>(cacheInfo.Response);
            var internalBatchDetail = System.Text.Json.JsonSerializer.Deserialize<BatchDetail>(cacheInfo.Response);
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
                        string path = Path.Combine(downloadPath, fileName);
                        if (!fileSystemHelper.CheckFileExists(path))
                        {
                            //var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, serviceConnectionString, internalBatchDetail.BatchId);

                            try
                            {
                                var containerName = internalBatchDetail.BatchId;
                                //await fileSystemHelper.DownloadToFileAsync(blobClient, path);
                                if (await azureBlobStorageClient.DownloadToFileAsync(serviceConnectionString, containerName, fileName,path))
                                {
                                    updateNumbers.Add(itemUpdateNumber.Value);
                                }
                                else
                                {
                                    logger.LogInformation(EventIds.DownloadENCFilesNonOkResponse.ToEventId(), "File not found for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId, fileName, fileItem);
                                }

                            }
                            catch (RequestFailedException requestFailedException) when (requestFailedException.ErrorCode == BlobErrorCode.BlobNotFound.ToString())
                            {
                                logger.LogError(EventIds.GetBlobDetailsWithCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId, fileName, fileItem, requestFailedException.Message);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(EventIds.DownloadENCFilesFromCacheContainerException.ToEventId(), "Error while download the file from blob for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} for blobName: {Name}, fileItem: {fileItem} with error: {Message}", item.ProductName, item.EditionNumber, itemUpdateNumber, businessUnit, queueMessage.BatchId, queueMessage.CorrelationId, fileName, fileItem, ex.Message);
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
            // rhz var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, serviceConnectionString, batchId);

            if (!await blobClient.ExistsAsync())
            {
                await blobClient.UploadAsync(stream);
            }
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

                await CopyFileToBlob(stream, $"{fssSearchResponseCache.BatchId}.json", fssSearchResponseCache.BatchId);
                fssSearchResponseCache.Response = string.Empty;
            }

            // rhz var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            //await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, fssCacheConfiguration.Value.FssSearchCacheTableName, storageConnectionString);
            await azureTableStorageClient.InsertOrMergeIntoTableStorageAsync(fssSearchResponseCache, serviceTableName, serviceConnectionString);
        }

        public async Task<Stream> DownloadFileFromCacheAsync(string fileName, string containerName)
        {
            // rhz var storageConnectionString = azureStorageService.GetStorageAccountConnectionString(fssCacheConfiguration.Value.CacheStorageAccountName, fssCacheConfiguration.Value.CacheStorageAccountKey);
            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName, serviceConnectionString, containerName);

            MemoryStream memoryStream = new MemoryStream();
            if (await blobClient.ExistsAsync())
            {
                await blobClient.DownloadToAsync(memoryStream);
            }
            return memoryStream;
        }
    }
}
