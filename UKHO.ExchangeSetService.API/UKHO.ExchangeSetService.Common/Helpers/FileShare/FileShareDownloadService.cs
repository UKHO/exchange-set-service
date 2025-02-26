// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers.FileShare.FileShareInterfaces;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers.FileShare
{
    public class FileShareDownloadService(
        ILogger<FileShareDownloadService> logger,
        IAuthFssTokenProvider authFssTokenProvider,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
        IFileShareServiceClient fileShareServiceClient,
        IFileSystemHelper fileSystemHelper,
        IOptions<CacheConfiguration> fssCacheConfiguration,
        IFileShareServiceCache fileShareServiceCache) : IFileShareDownloadService
    {
        private const string ServerHeaderValue = "Windows-Azure-Blob";
        private const string ReadMeContainerName = "readme";

        public async Task<bool> DownloadReadMeFileFromFssAsync(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var fileName = fileShareServiceConfig.Value.ReadMeFileName;
            var filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            var lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
            HttpResponseMessage httpReadMeFileResponse;
            httpReadMeFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, readMeFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpReadMeFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpReadMeFileResponse.Headers.Server.ToString().Split('/').First();
                using (var stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadReadmeFile307RedirectResponse.ToEventId(), "File share service download readme.txt redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    await fileShareServiceCache.CopyFileToBlob(stream, fileName, ReadMeContainerName);
                    return fileSystemHelper.DownloadReadmeFile(filePath, stream, lineToWrite);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadReadMeFileNonOkResponse.ToEventId(), "Error in file share service while downloading readme.txt file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpReadMeFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadReadMeFileNonOkResponse.ToEventId());
            }
        }

        public async Task<bool> DownloadReadMeFileFromCacheAsync(string batchId, string exchangeSetRootPath, string correlationId)
        {
            var isReadMeFileDownloaded = false;
            var fileName = fileShareServiceConfig.Value.ReadMeFileName;
            var filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            var lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
            using (var readmeStream = await fileShareServiceCache.DownloadFileFromCacheAsync(fileName, ReadMeContainerName))
            {
                if (readmeStream != null && readmeStream?.Length > 0)
                {
                    isReadMeFileDownloaded = fileSystemHelper.DownloadReadmeFile(filePath, readmeStream, lineToWrite);
                }
            }
            return isReadMeFileDownloaded;
        }

        public async Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string aioExchangeSetPath, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var fileName = fileShareServiceConfig.Value.IhoCrtFileName;
            var filePath = Path.Combine(aioExchangeSetPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(aioExchangeSetPath);
            HttpResponseMessage httpIhoCrtFileResponse;
            httpIhoCrtFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, ihoCrtFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpIhoCrtFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpIhoCrtFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpIhoCrtFileResponse.Headers.Server.ToString().Split('/').First();
                using (var stream = await httpIhoCrtFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadIhoCrtFile307RedirectResponse.ToEventId(), "File share service download IHO.crt redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    return fileSystemHelper.DownloadFile(filePath, stream);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadIhoCrtFileNonOkResponse.ToEventId(), "Error in file share service while downloading IHO.crt file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpIhoCrtFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadIhoCrtFileNonOkResponse.ToEventId());
            }
        }


        public async Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string aioExchangeSetPath, string correlationId)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var fileName = fileShareServiceConfig.Value.IhoPubFileName;
            var filePath = Path.Combine(aioExchangeSetPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(aioExchangeSetPath);
            HttpResponseMessage httpIhoPubFileResponse;
            httpIhoPubFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, ihoPubFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpIhoPubFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpIhoPubFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpIhoPubFileResponse.Headers.Server.ToString().Split('/').First();
                using (var stream = await httpIhoPubFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadIhoPubFile307RedirectResponse.ToEventId(), "File share service download IHO.pub redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    return fileSystemHelper.DownloadFile(filePath, stream);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadIhoPubFileNonOkResponse.ToEventId(), "Error in file share service while downloading IHO.pub file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpIhoPubFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadIhoPubFileNonOkResponse.ToEventId());
            }
        }

        public async Task<bool> DownloadBatchFiles(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile(entry, uri, downloadPath, payloadJson, accessToken, queueMessage, cancellationTokenSource, cancellationToken);
        }

        public async Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            foreach (var item in fileDetails)
            {
                var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, null, accessToken, item.Links.Get.Href, CancellationToken.None, correlationId);

                if (httpResponse.IsSuccessStatusCode)
                {
                    fileSystemHelper.CreateFileCopy(Path.Combine(exchangeSetPath, item.Filename), await httpResponse.Content.ReadAsStreamAsync());
                    logger.LogInformation(EventIds.DownloadInfoFolderFilesOkResponse.ToEventId(), "Successfully downloaded folder files for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                }
                else
                {
                    logger.LogError(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId(), "Error in file share service while searching folder files with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                    throw new FulfilmentException(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId());
                }
            }
            return true;
        }

        private async Task<bool> ProcessBatchFile(BatchDetail entry, IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var result = false;
            foreach (var item in uri)
            {
                var fileName = item.Split("/").Last();
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{Uri} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }
                var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item, CancellationToken.None, queueMessage.CorrelationId);

                var requestUri = new Uri(httpResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var serverValue = httpResponse.Headers.Server.ToString().Split('/').First();
                    fileSystemHelper.CheckAndCreateFolder(downloadPath);
                    var path = Path.Combine(downloadPath, fileName);
                    if (!fileSystemHelper.CheckFileExists(path) || CommonHelper.IsPeriodicOutputService)
                    {
                        await CopyFileToFolder(httpResponse, path, fileName, entry, queueMessage);
                        result = true;
                    }
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadENCFiles307RedirectResponse.ToEventId(), "File share service download ENC file:{fileName} redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fileName, requestUri, queueMessage.BatchId, queueMessage.CorrelationId);
                    }
                }
                else
                {
                    cancellationTokenSource.Cancel();
                    logger.LogError(EventIds.DownloadENCFilesNonOkResponse.ToEventId(), "Error in file share service while downloading ENC file:{fileName} with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, requestUri, httpResponse.StatusCode, queueMessage.BatchId, queueMessage.CorrelationId);
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in file share service while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, JsonConvert.SerializeObject(cancellationTokenSource.Token), requestUri, httpResponse.StatusCode, queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new FulfilmentException(EventIds.DownloadENCFilesNonOkResponse.ToEventId());
                }

            }
            if (fssCacheConfiguration.Value.IsFssCacheEnabled && !entry.IgnoreCache)
            {
                var productName = entry.Attributes.Where(a => a.Key == "CellName").Select(a => a.Value).FirstOrDefault();
                var editionNumber = entry.Attributes.Where(a => a.Key == "EditionNumber").Select(a => a.Value).FirstOrDefault();
                var updateNumber = entry.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();
                var businessUnit = entry.BusinessUnit;

                var fssSearchResponseCache = new FssSearchResponseCache
                {
                    BatchId = entry.BatchId,
                    PartitionKey = productName,
                    RowKey = $"{editionNumber}|{updateNumber}|{businessUnit}",
                    Response = JsonConvert.SerializeObject(entry)
                };
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
                    "File share service search response insert/merge request in azure table for cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber}, UpdateNumber:{UpdateNumber} and BusinessUnit:{BusinessUnit} with FSS BatchId:{FssBatchId}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                        return result;
                    }, productName, editionNumber, updateNumber, businessUnit, entry.BatchId, queueMessage.BatchId, queueMessage.CorrelationId);
            }
            return result;
        }

        private async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path, string fileName, BatchDetail entry, SalesCatalogueServiceResponseQueueMessage queueMessage)
        {
            var bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());
            fileSystemHelper.CreateFileCopy(path, new MemoryStream(bytes));
            if (!entry.IgnoreCache && fssCacheConfiguration.Value.IsFssCacheEnabled)
            {
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceUploadENCFilesToCacheStart, EventIds.FileShareServiceUploadENCFilesToCacheCompleted,
                    "File share service upload ENC file request to cache blob container for Container:{Container}, with FileName: {FileName}. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        await fileShareServiceCache.CopyFileToBlob(new MemoryStream(bytes), fileName, entry.BatchId);
                        return Task.CompletedTask;
                    }, entry.BatchId, fileName, queueMessage.BatchId, queueMessage.CorrelationId);
            }
        }

    }
}
