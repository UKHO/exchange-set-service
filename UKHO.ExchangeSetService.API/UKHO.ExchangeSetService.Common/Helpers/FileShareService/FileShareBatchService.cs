// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers.Auth;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareBatchService(ILogger<FileShareService> logger, IMonitorHelper monitorHelper, IAuthFssTokenProvider authFssTokenProvider,
        IOptions<FileShareServiceConfiguration> fileShareServiceConfig, IFileShareServiceClient fileShareServiceClient,
                                IOptions<AioConfiguration> aioConfiguration, IFileSystemHelper fileSystemHelper,
                                IOptions<CacheConfiguration> fssCacheConfiguration,
                                IFileShareServiceCache fileShareServiceCache,
                                IFileShareDownloadService fileShareDownloadService) : IFileShareBatchService
    {
        private const string ZIPFILE = "zip";

        public async Task<CreateBatchResponse> CreateBatch(string userOid, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var uri = $"/batch";

            var createBatchRequest = CreateBatchRequest(userOid);

            var payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Post, payloadJson, accessToken, uri, CancellationToken.None);

            var createBatchResponse = await CreateBatchResponse(httpResponse, createBatchRequest.ExpiryDate, correlationId);
            return createBatchResponse;
        }

        //Commit and check batch status of ENC / AIO / Error.txt batch to FSS
        public async Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = ZIPFILE)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            var isBatchCommitted = false;
            var batchCommitMetaDataList = new List<BatchCommitMetaData>();

            if (fileName == ZIPFILE)
            {
                //Get zip files full path
                var exchangeSetZipList = fileSystemHelper.GetFiles(exchangeSetZipPath).Where(di => di.Contains(ZIPFILE)).ToArray();

                foreach (var zipFileName in exchangeSetZipList)
                {
                    var customFileInfo = fileSystemHelper.GetFileInfo(zipFileName);

                    var batchCommitMetaData = new BatchCommitMetaData()
                    {
                        BatchId = batchId,
                        AccessToken = accessToken,
                        FileName = customFileInfo.Name,
                        FullFileName = customFileInfo.FullName
                    };

                    batchCommitMetaDataList.Add(batchCommitMetaData);
                }
            }
            else
            {
                var customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipPath, fileName));

                var batchCommitMetaData = new BatchCommitMetaData()
                {
                    BatchId = batchId,
                    AccessToken = accessToken,
                    FileName = customFileInfo.Name,
                    FullFileName = customFileInfo.FullName
                };

                batchCommitMetaDataList.Add(batchCommitMetaData);
            }

            var batchStatus = await CommitAndGetBatchStatus(batchId, correlationId, accessToken, batchCommitMetaDataList);
            if (batchStatus == BatchStatus.Committed)
            {
                isBatchCommitted = true;
            }
            logger.LogInformation(EventIds.BatchStatus.ToEventId(), "BatchStatus:{batchStatus} for file:{fileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, GetFileNameOfCommittedBatch(batchCommitMetaDataList), batchId, correlationId);
            return isBatchCommitted;
        }

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath, string businessUnit)
        {
            var internalSearchBatchResponse = new SearchBatchResponse
            {
                Entries = new List<BatchDetail>()
            };

            var cacheProductsNotFound = fssCacheConfiguration.Value.IsFssCacheEnabled ? await fileShareServiceCache.GetNonCachedProductDataForFss(products, internalSearchBatchResponse, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken, businessUnit) : products;

            if (cacheProductsNotFound != null && cacheProductsNotFound.Any())
            {
                var internalNotFoundProducts = new List<Products>();
                var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
                var productWithAttributes = GenerateQueryForFss(cacheProductsNotFound);
                var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter=BusinessUnit eq '{businessUnit}' and {fileShareServiceConfig.Value.ProductCode} {productWithAttributes.Item1}";

                HttpResponseMessage httpResponse;

                var payloadJson = string.Empty;
                var productList = new List<string>();
                var prodCount = products.Select(a => a.UpdateNumbers).Sum(a => a.Count);
                var queryCount = 0;

                return await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchDownloadForENCFilesStart,
                EventIds.FileShareServiceSearchDownloadForENCFilesCompleted,
                "File share service search and download request for {productDetails}. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    do
                    {
                        queryCount++;
                        if (cancellationToken.IsCancellationRequested)
                        {
                            logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files with cancellationToken:{cancellationTokenSource.Token} and uri:{Uri} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, message.BatchId, message.CorrelationId);
                            throw new OperationCanceledException();
                        }
                        httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, cancellationToken, message.CorrelationId);

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            uri = await SelectLatestPublishedDateBatch(cacheProductsNotFound, internalSearchBatchResponse, httpResponse, productList, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                        }
                        else
                        {
                            cancellationTokenSource.Cancel();
                            logger.LogError(EventIds.QueryFileShareServiceENCFilesNonOkResponse.ToEventId(), "Error in file share service while searching ENC files with uri:{RequestUri}, responded with {StatusCode} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, message.BatchId, message.CorrelationId);
                            logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in file share service while searching ENC files with cancellationToken:{cancellationTokenSource.Token} with uri:{RequestUri}, responded with {StatusCode} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, message.BatchId, message.CorrelationId);
                            throw new FulfilmentException(EventIds.QueryFileShareServiceENCFilesNonOkResponse.ToEventId());
                        }

                    } while (httpResponse.IsSuccessStatusCode && internalSearchBatchResponse.Entries.Count != 0 && internalSearchBatchResponse.Entries.Count < prodCount && !string.IsNullOrWhiteSpace(uri));
                    internalSearchBatchResponse.QueryCount = queryCount;
                    CheckProductsExistsInFileShareService(products, message.CorrelationId, message.BatchId, internalSearchBatchResponse, internalNotFoundProducts, prodCount, cancellationTokenSource, cancellationToken);
                    return internalSearchBatchResponse;
                }, productWithAttributes.Item2, message.BatchId, message.CorrelationId);
            }
            return internalSearchBatchResponse;
        }

        public async Task<bool> CommitAndGetBatchStatusForLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            var batchCommitMetaDataList = new List<BatchCommitMetaData>();

            //Get zip files full path
            var mediaZipList = fileSystemHelper.GetFiles(exchangeSetZipPath).Where(di => di.Contains("zip")).ToArray();

            foreach (var fileName in mediaZipList)
            {
                var customFileInfo = fileSystemHelper.GetFileInfo(fileName);

                var batchCommitMetaData = new BatchCommitMetaData()
                {
                    BatchId = batchId,
                    AccessToken = accessToken,
                    FileName = customFileInfo.Name,
                    FullFileName = customFileInfo.FullName
                };

                batchCommitMetaDataList.Add(batchCommitMetaData);
            }

            var commitTaskStartedAt = DateTime.UtcNow;
            var isUploadCommitBatchCompleted = await UploadCommitBatchForLargeMediaExchangeSet(batchCommitMetaDataList, correlationId);
            var batchStatus = BatchStatus.CommitInProgress;
            if (isUploadCommitBatchCompleted)
            {
                var batchStatusMetaData = new BatchStatusMetaData()
                {
                    AccessToken = accessToken,
                    BatchId = batchId
                };
                var watch = new Stopwatch();
                watch.Start();
                while (batchStatus != BatchStatus.Committed && watch.Elapsed.TotalMinutes <= fileShareServiceConfig.Value.PosBatchCommitCutOffTimeInMinutes)
                {
                    batchStatus = await GetLargeMediaBatchStatus(batchStatusMetaData, correlationId);
                    if (batchStatus == BatchStatus.Failed)
                    {
                        watch.Stop();
                        logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());
                    }
                    await Task.Delay(fileShareServiceConfig.Value.PosBatchCommitDelayTimeInMilliseconds);
                }
                if (batchStatus != BatchStatus.Committed)
                {
                    watch.Stop();
                    logger.LogError(EventIds.BatchCommitTimeout.ToEventId(), "Batch Commit Status timeout for large media exchange set with BatchStatus:{batchStatus} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, batchStatusMetaData.BatchId, correlationId);
                    throw new FulfilmentException(EventIds.BatchCommitTimeout.ToEventId());
                }
                watch.Stop();
            }
            else
            {
                logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());
            }
            var commitTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Commit Batch Task", commitTaskStartedAt, commitTaskCompletedAt, correlationId, null, null, null, batchId);

            logger.LogInformation(EventIds.BatchStatus.ToEventId(), "BatchStatus:{batchStatus} for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, batchId, correlationId);

            return batchStatus == BatchStatus.Committed;
        }

        private CreateBatchRequest CreateBatchRequest(string oid)
        {
            var createBatchRequest = new CreateBatchRequest
            {
                BusinessUnit = fileShareServiceConfig.Value.EssBusinessUnit,
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Update"),
                    new KeyValuePair<string, string>("Media Type", "Zip"),
                    new KeyValuePair<string, string>("Product Type", "AVCS")
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { oid }
                }
            };

            return createBatchRequest;
        }

        private async Task<CreateBatchResponse> CreateBatchResponse(HttpResponseMessage httpResponse, string batchExpiryDateTime, string correlationId)
        {
            var createBatchResponse = new CreateBatchResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(EventIds.FSSCreateBatchNonOkResponse.ToEventId(), "Error in file share service create batch endpoint with Uri:{RequestUri} responded with {StatusCode} for _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = null;
            }
            else
            {
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = JsonConvert.DeserializeObject<CreateBatchResponseModel>(body);
                createBatchResponse.ResponseBody.BatchStatusUri = $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{createBatchResponse.ResponseBody.BatchId}/status";
                createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri = $"{fileShareServiceConfig.Value.PublicBaseUrl}/batch/{createBatchResponse.ResponseBody.BatchId}";
                createBatchResponse.ResponseBody.BatchExpiryDateTime = batchExpiryDateTime;
                createBatchResponse.ResponseBody.ExchangeSetFileUri = $"{createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri}/files/{fileShareServiceConfig.Value.ExchangeSetFileName}";
                createBatchResponse.ResponseBody.AioExchangeSetFileUri = $"{createBatchResponse.ResponseBody.ExchangeSetBatchDetailsUri}/files/{fileShareServiceConfig.Value.AioExchangeSetFileName}";
            }

            return createBatchResponse;
        }

        private Task<bool> UploadCommitBatchForLargeMediaExchangeSet(List<BatchCommitMetaData> batchCommitMetaDataList, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadCommitBatchStart, EventIds.UploadCommitBatchCompleted,
                "Upload Commit Batch for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var fileDetails = fileSystemHelper.UploadLargeMediaCommitBatch(batchCommitMetaDataList);
                    var batchCommitModel = new BatchCommitModel()
                    {
                        FileDetails = fileDetails
                    };

                    var httpResponse = await fileShareServiceClient.CommitBatchAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, batchCommitMetaDataList[0].BatchId, batchCommitModel, batchCommitMetaDataList[0].AccessToken, correlationId);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.UploadCommitBatchNonOkResponse.ToEventId(), "Error in Upload Commit Batch for large media exchange set with uri:{RequestUri} responded with {StatusCode} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchCommitMetaDataList[0].BatchId, correlationId);
                        throw new FulfilmentException(EventIds.UploadCommitBatchNonOkResponse.ToEventId());
                    }
                    return true;
                }, batchCommitMetaDataList[0].BatchId, correlationId);
        }

        private Task<BatchStatus> GetLargeMediaBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.GetBatchStatusStart, EventIds.GetBatchStatusCompleted,
                "Get Batch Status for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var httpResponse = await fileShareServiceClient.GetBatchStatusAsync(HttpMethod.Get, fileShareServiceConfig.Value.BaseUrl, batchStatusMetaData.BatchId, batchStatusMetaData.AccessToken);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.GetBatchStatusNonOkResponse.ToEventId(), "Error in Get Batch Status for large media exchange set with uri:{RequestUri} responded with {StatusCode} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.GetBatchStatusNonOkResponse.ToEventId());
                    }
                    var bodyJson = await httpResponse.Content.ReadAsStringAsync();
                    var responseBatchStatusModel = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(bodyJson);
                    return (BatchStatus)Enum.Parse(typeof(BatchStatus), responseBatchStatusModel.Status.ToString());
                }, batchStatusMetaData.BatchId, correlationId);
        }

        private async Task<BatchStatus> CommitAndGetBatchStatus(string batchId, string correlationId, string accessToken, List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            var commitTaskStartedAt = DateTime.UtcNow;
            var isUploadCommitBatchCompleted = await UploadCommitBatch(batchCommitMetaDataList, correlationId);
            var batchStatus = BatchStatus.CommitInProgress;
            if (isUploadCommitBatchCompleted)
            {
                var batchStatusMetaData = new BatchStatusMetaData()
                {
                    AccessToken = accessToken,
                    BatchId = batchId
                };
                var watch = new Stopwatch();
                watch.Start();
                while (batchStatus != BatchStatus.Committed && watch.Elapsed.TotalMinutes <= fileShareServiceConfig.Value.BatchCommitCutOffTimeInMinutes)
                {
                    batchStatus = await GetBatchStatus(batchStatusMetaData, correlationId);
                    if (batchStatus == BatchStatus.Failed)
                    {
                        watch.Stop();
                        logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());

                    }
                    await Task.Delay(fileShareServiceConfig.Value.BatchCommitDelayTimeInMilliseconds);
                }
                if (batchStatus != BatchStatus.Committed)
                {
                    watch.Stop();
                    logger.LogError(EventIds.BatchCommitTimeout.ToEventId(), "Batch Commit Status timeout with BatchStatus:{batchStatus} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, batchStatusMetaData.BatchId, correlationId);
                    throw new FulfilmentException(EventIds.BatchCommitTimeout.ToEventId());
                }
                watch.Stop();
            }
            else
            {
                logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());
            }
            var commitTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Commit Batch Task", commitTaskStartedAt, commitTaskCompletedAt, correlationId, null, null, null, batchId);
            return batchStatus;
        }

        private void CheckProductsExistsInFileShareService(List<Products> products, string correlationId, string batchId, SearchBatchResponse internalSearchBatchResponse, List<Products> internalNotFoundProducts, int prodCount, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), batchId, correlationId);
                throw new OperationCanceledException();
            }

            if (internalSearchBatchResponse.Entries.Any() && prodCount != internalSearchBatchResponse.Entries.Count)
            {
                var internalProducts = new List<Products>();
                ConvertFssSearchBatchResponseToProductResponse(internalSearchBatchResponse, internalProducts);
                GetProductDetailsNotFoundInFileShareService(products, internalNotFoundProducts, internalProducts);
            }
            if (internalNotFoundProducts.Any() || !internalSearchBatchResponse.Entries.Any())
            {
                var internalNotFoundProductsPayLoadJson = JsonConvert.SerializeObject(internalNotFoundProducts.Any() ? internalNotFoundProducts.Distinct() : products);
                cancellationTokenSource.Cancel();
                logger.LogError(EventIds.FSSResponseNotFoundForRespectiveProductWhileQuerying.ToEventId(), "Error in file share service while searching ENC files and no data found while querying for products:{internalNotFoundProductsPayLoadJson} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", internalNotFoundProductsPayLoadJson, batchId, correlationId);
                logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in file share service while searching ENC files and no data found while querying for products:{internalNotFoundProductsPayLoadJson} CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", internalNotFoundProductsPayLoadJson, JsonConvert.SerializeObject(cancellationTokenSource.Token), batchId, correlationId);
                throw new FulfilmentException(EventIds.FSSResponseNotFoundForRespectiveProductWhileQuerying.ToEventId());
            }
        }

        private void GetProductDetailsNotFoundInFileShareService(List<Products> products, List<Products> internalNotFoundProducts, List<Products> internalProducts)
        {
            foreach (var itemProduct in products)
            {
                foreach (var itemUpdateNumber in itemProduct.UpdateNumbers)
                {
                    var checkNoDataFound = internalProducts.Where(a => a.EditionNumber == itemProduct.EditionNumber && a.ProductName == itemProduct.ProductName).Select(a => a.UpdateNumbers);
                    if (checkNoDataFound != null && !checkNoDataFound.Any(a => a.Contains(itemUpdateNumber)))
                    {
                        internalNotFoundProducts.Add(new Products
                        {
                            EditionNumber = itemProduct.EditionNumber,
                            ProductName = itemProduct.ProductName,
                            Cancellation = itemProduct.Cancellation,
                            Dates = itemProduct.Dates,
                            FileSize = itemProduct.FileSize,
                            UpdateNumbers = new List<int?> { itemUpdateNumber },
                            Bundle = itemProduct.Bundle
                        });
                    }
                }
            }
        }

        private void ConvertFssSearchBatchResponseToProductResponse(SearchBatchResponse internalSearchBatchResponse, List<Products> internalProducts)
        {
            foreach (var item in internalSearchBatchResponse.Entries)
            {
                var product = new Products
                {
                    EditionNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "EditionNumber").Select(b => b.Value).FirstOrDefault()),
                    ProductName = item.Attributes?.Where(a => a.Key == "CellName").Select(b => b.Value).FirstOrDefault()
                };
                if (product.UpdateNumbers == null)
                {
                    product.UpdateNumbers = new List<int?>();
                }
                var UpdateNumber = Convert.ToInt32(item.Attributes?.Where(a => a.Key == "UpdateNumber").Select(b => b.Value).FirstOrDefault());
                product.UpdateNumbers.Add(UpdateNumber);
                internalProducts.Add(product);
            }
        }

        private async Task<string> SelectLatestPublishedDateBatch(List<Products> products, SearchBatchResponse internalSearchBatchResponse, HttpResponseMessage httpResponse, List<string> productList, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var searchBatchResponse = await SearchBatch(httpResponse);
            foreach (var item in searchBatchResponse.Entries)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                    throw new OperationCanceledException();
                }
                foreach (var productItem in products)
                {
                    var matchProduct = item.Attributes.Where(a => a.Key == "UpdateNumber");
                    var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
                    var compareProducts = $"{productItem.ProductName}|{productItem.EditionNumber}|{updateNumber}";
                    if (!productList.Contains(compareProducts))
                    {
                        await CheckProductOrCancellationData(internalSearchBatchResponse, productList, item, productItem, updateNumber, compareProducts, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                    }
                }
            }

            return searchBatchResponse.Links?.Next?.Href;
        }

        private async Task CheckProductOrCancellationData(SearchBatchResponse internalSearchBatchResponse, List<string> productList, BatchDetail item, Products productItem, string updateNumber, string compareProducts, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var aioCells = !string.IsNullOrEmpty(aioConfiguration.Value.AioCells) ? new(aioConfiguration.Value.AioCells.Split(',')) : new List<string>();

            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), message.BatchId, message.CorrelationId);
                throw new OperationCanceledException();
            }
            if (CheckProductDoesExistInResponseItem(item, productItem) && productItem.Cancellation != null && productItem.Cancellation.UpdateNumber.HasValue
                                    && Convert.ToInt32(updateNumber) == productItem.Cancellation.UpdateNumber.Value)
            {
                await CheckProductWithCancellationData(internalSearchBatchResponse, productList, item, productItem, compareProducts, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
            }
            else if (CheckProductDoesExistInResponseItem(item, productItem)
            && CheckEditionNumberDoesExistInResponseItem(item, productItem) && CheckUpdateNumberDoesExistInResponseItem(item, productItem))
            {
                internalSearchBatchResponse.Entries.Add(item);
                productList.Add(compareProducts);
                await DownloadEncFilesFromFssBatch(item, productItem, message, cancellationTokenSource, exchangeSetRootPath, aioCells, cancellationToken);
            }
        }

        private async Task DownloadEncFilesFromFssBatch(BatchDetail item, Products productItem, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, string exchangeSetRootPath, List<string> aioCells, CancellationToken cancellationToken)
        {
            if (CommonHelper.IsLargeLayout)
            {
                if (!aioCells.Contains(productItem.ProductName))
                {
                    await PerformBatchFileDownloadForLargeMediaExchangeSet(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
                }
                else
                {
                    await PerformBatchFileDownload(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
                }

            }
            else
            {
                await PerformBatchFileDownload(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
            }
        }

        private async Task CheckProductWithCancellationData(SearchBatchResponse internalSearchBatchResponse, List<string> productList, BatchDetail item, Products productItem, string compareProducts, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var matchEditionNumber = item.Attributes.Where(a => a.Key == "EditionNumber").ToList();
            if (matchEditionNumber.Any(a => a.Value == productItem.Cancellation.EditionNumber.Value.ToString()))
            {
                matchEditionNumber.ForEach(c => c.Value = Convert.ToString(productItem.EditionNumber));
                item.IgnoreCache = true;
                internalSearchBatchResponse.Entries.Add(item);
                productList.Add(compareProducts);
                if (CommonHelper.IsLargeLayout)
                    await PerformBatchFileDownloadForLargeMediaExchangeSet(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
                else
                    await PerformBatchFileDownload(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
            }
        }

        private Task PerformBatchFileDownload(BatchDetail item, Products productItem, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var productName = productItem.ProductName;
            var editionNumber = Convert.ToString(productItem.EditionNumber);
            var updateNumber = item.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadENCFilesRequestStart, EventIds.DownloadENCFilesRequestCompleted,
                "File share service download request for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var downloadPath = Path.Combine(exchangeSetRootPath, productName.Substring(0, 2), productName, editionNumber, updateNumber);
                    return await fileShareDownloadService.DownloadBatchFiles(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
                }, productName, editionNumber, updateNumber, item.Files.Select(a => a.Links.Get.Href), message.BatchId, message.CorrelationId);
        }

        private Task PerformBatchFileDownloadForLargeMediaExchangeSet(BatchDetail item, Products productItem, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var productName = productItem.ProductName;
            var editionNumber = Convert.ToString(productItem.EditionNumber);
            var updateNumber = item.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();
            var bundleInfo = productItem.Bundle.FirstOrDefault()?.Location.Split(";");

            if (bundleInfo is null)
            {
                //In the "unlikely" event that bundleInfo is null
                logger.LogError(EventIds.DownloadENCFilesRequestStart.ToEventId(), "Products.Bundle is null in file share service download request for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and _X-Correlation-ID:{CorrelationId}", productName, editionNumber, message.CorrelationId);
                return Task.FromResult(false);
            }

            exchangeSetRootPath = string.Format(exchangeSetRootPath, bundleInfo[0].Substring(1, 1), bundleInfo[1]);

            return logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadENCFilesRequestStart, EventIds.DownloadENCFilesRequestCompleted,
                "File share service download request for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var downloadPath = Path.Combine(exchangeSetRootPath, productName.Substring(0, 2), productName, editionNumber);
                    return await fileShareDownloadService.DownloadBatchFiles(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
                }, productName, editionNumber, updateNumber, item.Files.Select(a => a.Links.Get.Href), message.BatchId, message.CorrelationId);
        }

        private (string, string) GenerateQueryForFss(List<Products> products)
        {
            var sb = new StringBuilder();
            var sbLog = new StringBuilder();
            if (products != null && products.Any())
            {
                var productCount = products.Count;
                var productIndex = 1;

                sb.Append("(");////1st main (
                foreach (var item in products)
                {
                    var itemSb = new StringBuilder();
                    var cancellation = new StringBuilder();
                    itemSb.Append("(");////1st product
                    itemSb.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                    itemSb.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.EditionNumber);
                    var updateNumbers = string.Empty;
                    if (item.UpdateNumbers != null && item.UpdateNumbers.Any())
                    {
                        var lstCount = item.UpdateNumbers.Count;
                        var index = 1;

                        foreach (var updateNumberItem in item.UpdateNumbers)
                        {
                            if (index == 1)
                            {
                                itemSb.Append("((");
                            }
                            if (item.Cancellation != null && item.Cancellation.UpdateNumber == updateNumberItem.Value)
                            {
                                cancellation.Append(" or (");////1st cancellation product
                                cancellation.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                                cancellation.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.Cancellation.EditionNumber);
                                cancellation.AppendFormat(fileShareServiceConfig.Value.UpdateNumber, item.Cancellation.UpdateNumber);
                                cancellation.Append(")");
                                item.IgnoreCache = true;
                            }
                            itemSb.AppendFormat(fileShareServiceConfig.Value.UpdateNumber, updateNumberItem.Value);
                            itemSb.Append(lstCount != index ? "or " : "))");
                            index += 1;
                        }

                        updateNumbers = string.Join(",", item.UpdateNumbers.Select(a => a.Value.ToString()));
                    }
                    itemSb.Append(cancellation + (productCount == productIndex ? ")" : ") or "));/////last product or with multiple
                    sbLog.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1} and UpdateNumbers:[{2}]", item.ProductName, item.EditionNumber.ToString(), updateNumbers);
                    if (cancellation.Length > 0)
                    {
                        sbLog.AppendFormat("\n with Cancellation Product/CellName:{0}, EditionNumber:{1} and UpdateNumber:{2}", item.ProductName, item.Cancellation.EditionNumber.ToString(), item.Cancellation.UpdateNumber.ToString());
                    }
                    productIndex += 1;
                    sb.Append(itemSb.ToString());
                }
                sb.Append(")");//// last main )
            }
            return (sb.ToString(), sbLog.ToString());
        }
        private static string GetFileNameOfCommittedBatch(List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            return batchCommitMetaDataList.ElementAtOrDefault(1) == null ? batchCommitMetaDataList[0].FileName :
                            batchCommitMetaDataList[0].FileName + " and " + batchCommitMetaDataList[1].FileName;
        }

        private bool CheckProductDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "CellName" && a.Value == product.ProductName);
        }

        private bool CheckEditionNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "EditionNumber" && product.EditionNumber.Value.ToString() == a.Value);
        }

        private bool CheckUpdateNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            var matchProduct = batchDetail.Attributes.Where(a => a.Key == "UpdateNumber");
            var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
            return product.UpdateNumbers.Any(x => x.Value.ToString() == updateNumber);
        }

        private async Task<SearchBatchResponse> SearchBatch(HttpResponseMessage httpResponse)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }

        private Task<bool> UploadCommitBatch(List<BatchCommitMetaData> batchCommitMetaDataList, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadCommitBatchStart, EventIds.UploadCommitBatchCompleted,
                "Upload Commit Batch for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var fileDetails = fileSystemHelper.UploadLargeMediaCommitBatch(batchCommitMetaDataList);
                    var batchCommitModel = new BatchCommitModel()
                    {
                        FileDetails = fileDetails
                    };

                    HttpResponseMessage httpResponse;
                    httpResponse = await fileShareServiceClient.CommitBatchAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, batchCommitMetaDataList[0].BatchId, batchCommitModel, batchCommitMetaDataList[0].AccessToken, correlationId);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.UploadCommitBatchNonOkResponse.ToEventId(), "Error in Upload Commit Batch with uri:{RequestUri} responded with {StatusCode} for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, GetFileNameOfCommittedBatch(batchCommitMetaDataList), batchCommitMetaDataList[0].BatchId, correlationId);
                        throw new FulfilmentException(EventIds.UploadCommitBatchNonOkResponse.ToEventId());
                    }
                    return true;
                }, GetFileNameOfCommittedBatch(batchCommitMetaDataList),
                batchCommitMetaDataList[0].BatchId, correlationId);
        }

        private Task<BatchStatus> GetBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.GetBatchStatusStart, EventIds.GetBatchStatusCompleted,
                "Get Batch Status for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    HttpResponseMessage httpResponse;
                    httpResponse = await fileShareServiceClient.GetBatchStatusAsync(HttpMethod.Get, fileShareServiceConfig.Value.BaseUrl, batchStatusMetaData.BatchId, batchStatusMetaData.AccessToken);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.GetBatchStatusNonOkResponse.ToEventId(), "Error in Get Batch Status with uri:{RequestUri} responded with {StatusCode} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.GetBatchStatusNonOkResponse.ToEventId());
                    }
                    string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                    ResponseBatchStatusModel responseBatchStatusModel = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(bodyJson);
                    return (BatchStatus)Enum.Parse(typeof(BatchStatus), responseBatchStatusModel.Status.ToString());
                }, batchStatusMetaData.BatchId, correlationId);
        }
    }
}
