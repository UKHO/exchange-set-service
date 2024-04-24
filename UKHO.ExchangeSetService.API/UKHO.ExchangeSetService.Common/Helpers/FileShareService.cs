using Azure.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureTableEntities;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Enums;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService : IFileShareService
    {
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IAuthFssTokenProvider authFssTokenProvider;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly ILogger<FileShareService> logger;
        private readonly IFileShareServiceCache fileShareServiceCache;
        private readonly IOptions<CacheConfiguration> fssCacheConfiguration;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly IMonitorHelper monitorHelper;
        private readonly AioConfiguration aioConfiguration;
        private const string ServerHeaderValue = "Windows-Azure-Blob";
        private const string ZIPFILE = "zip";

        public FileShareService(IFileShareServiceClient fileShareServiceClient,
                                IAuthFssTokenProvider authFssTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                ILogger<FileShareService> logger,
                                IFileShareServiceCache fileShareServiceCache,
                                IOptions<CacheConfiguration> fssCacheConfiguration,
                                IFileSystemHelper fileSystemHelper, IMonitorHelper monitorHelper,
                                IOptions<AioConfiguration> aioConfiguration)
        {
            this.fileShareServiceClient = fileShareServiceClient;
            this.authFssTokenProvider = authFssTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
            this.fileShareServiceCache = fileShareServiceCache;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
            this.monitorHelper = monitorHelper;
            this.aioConfiguration = aioConfiguration.Value;
        }

        public async Task<CreateBatchResponse> CreateBatch(string userOid, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var uri = $"/batch";

            CreateBatchRequest createBatchRequest = CreateBatchRequest(userOid);

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Post, payloadJson, accessToken, uri, CancellationToken.None);

            CreateBatchResponse createBatchResponse = await CreateBatchResponse(httpResponse, createBatchRequest.ExpiryDate, correlationId);
            return createBatchResponse;
        }

        private CreateBatchRequest CreateBatchRequest(string oid)
        {
            var createBatchRequest = new CreateBatchRequest
            {
                BusinessUnit = fileShareServiceConfig.Value.BusinessUnit,
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

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var internalSearchBatchResponse = new SearchBatchResponse
            {
                Entries = new List<BatchDetail>()
            };
            List<Products> cacheProductsNotFound = fssCacheConfiguration.Value.IsFssCacheEnabled ? await fileShareServiceCache.GetNonCachedProductDataForFss(products, internalSearchBatchResponse, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken) : products;

            if (cacheProductsNotFound != null && cacheProductsNotFound.Any())
            {
                var internalNotFoundProducts = new List<Products>();
                var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
                var productWithAttributes = GenerateQueryForFss(cacheProductsNotFound);
                var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter=BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}' and {fileShareServiceConfig.Value.ProductCode} {productWithAttributes.Item1}";

                HttpResponseMessage httpResponse;

                string payloadJson = string.Empty;
                var productList = new List<string>();
                var prodCount = products.Select(a => a.UpdateNumbers).Sum(a => a.Count);
                int queryCount = 0;

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


        public async Task<(SearchBatchResponse, List<(string fileName, string filePath, byte[] fileContent)>)> GetBatchInfoBasedOnProducts1 (List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var internalSearchBatchResponse = new SearchBatchResponse
            {
                Entries = new List<BatchDetail>()
            };
            //List<Products> cacheProductsNotFound = fssCacheConfiguration.Value.IsFssCacheEnabled ? await fileShareServiceCache.GetNonCachedProductDataForFss(products, internalSearchBatchResponse, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken) : products;
            var ListOfProducts = await fileShareServiceCache.GetNonCachedProductDataForFss1(products, internalSearchBatchResponse, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
            List<Products> cacheProductsNotFound = fssCacheConfiguration.Value.IsFssCacheEnabled ? ListOfProducts.NotFound: products;
            
            if (cacheProductsNotFound != null && cacheProductsNotFound.Any())
            {
                var internalNotFoundProducts = new List<Products>();
                var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

                var productWithAttributes = GenerateQueryForFss(cacheProductsNotFound);
                var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter=BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}' and {fileShareServiceConfig.Value.ProductCode} {productWithAttributes.Item1}";

                HttpResponseMessage httpResponse;

                string payloadJson = string.Empty;
                var productList = new List<string>();
                var prodCount = products.Select(a => a.UpdateNumbers).Sum(a => a.Count);
                int queryCount = 0;

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
                            //uri = await SelectLatestPublishedDateBatch(ListOfProducts.NotFound, internalSearchBatchResponse, httpResponse, productList, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                            var latestData = await SelectLatestPublishedDateBatch1(ListOfProducts.NotFound, internalSearchBatchResponse, httpResponse, productList, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                            uri = latestData.Item1;
                            foreach (var fileItem in latestData.Item2)
                            {
                                ListOfProducts.Found.Add((fileItem.FileName,
                                fileItem.FilePath,
                                fileItem.FileContent));
                            }
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
                    return (internalSearchBatchResponse, ListOfProducts.Found);
                }, productWithAttributes.Item2, message.BatchId, message.CorrelationId);
            }
            return (internalSearchBatchResponse, ListOfProducts.Found);
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
            SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
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

        private async Task<(string, List<FileDetails>)> SelectLatestPublishedDateBatch1(List<Products> products, SearchBatchResponse internalSearchBatchResponse, HttpResponseMessage httpResponse, List<string> productList, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            var fileContents = new List<FileDetails>();
            SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
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
                        var fileDetails = await CheckProductOrCancellationData1(internalSearchBatchResponse, productList, item, productItem, updateNumber, compareProducts, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
                        fileContents.AddRange(fileDetails);
                    }
                }
            }

            return (searchBatchResponse.Links?.Next?.Href, fileContents);
        }

        private async Task CheckProductOrCancellationData(SearchBatchResponse internalSearchBatchResponse, List<string> productList, BatchDetail item, Products productItem, string updateNumber, string compareProducts, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();

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

        private async Task<List<FileDetails>> CheckProductOrCancellationData1(SearchBatchResponse internalSearchBatchResponse, List<string> productList, BatchDetail item, Products productItem, string updateNumber, string compareProducts, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            List<string> aioCells = !string.IsNullOrEmpty(aioConfiguration.AioCells) ? new(aioConfiguration.AioCells.Split(',')) : new List<string>();
            List<FileDetails> fileContentList = new List<FileDetails>();
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
                fileContentList = await DownloadEncFilesFromFssBatch1(item, productItem, message, cancellationTokenSource, exchangeSetRootPath, aioCells, cancellationToken);
            }
            return fileContentList;
        }

        private async Task DownloadEncFilesFromFssBatch(BatchDetail item, Products productItem, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, string exchangeSetRootPath, List<string> aioCells, CancellationToken cancellationToken)
        {
            if (CommonHelper.IsPeriodicOutputService)
            {
                if (aioConfiguration.IsAioEnabled)
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
                    await PerformBatchFileDownloadForLargeMediaExchangeSet(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
                }
            }
            else
            {
                await PerformBatchFileDownload(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
            }
        }

        private async Task<List<FileDetails>> DownloadEncFilesFromFssBatch1(BatchDetail item, Products productItem, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, string exchangeSetRootPath, List<string> aioCells, CancellationToken cancellationToken)
        {
            List<FileDetails> fileList = new List<FileDetails>();
            if (CommonHelper.IsPeriodicOutputService)
            {
                if (aioConfiguration.IsAioEnabled)
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
                    await PerformBatchFileDownloadForLargeMediaExchangeSet(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
                }
            }
            else
            {
                fileList = await PerformBatchFileDownload1(item, productItem, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken);
            }

            return fileList;
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
                if (CommonHelper.IsPeriodicOutputService)
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
                    return await DownloadBatchFiles(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
                }, productName, editionNumber, updateNumber, item.Files.Select(a => a.Links.Get.Href), message.BatchId, message.CorrelationId);
        }
        private Task<List<FileDetails>> PerformBatchFileDownload1(BatchDetail item, Products productItem, string exchangeSetRootPath, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            var productName = productItem.ProductName;
            var editionNumber = Convert.ToString(productItem.EditionNumber);
            var updateNumber = item.Attributes.Where(a => a.Key == "UpdateNumber").Select(a => a.Value).FirstOrDefault();
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadENCFilesRequestStart, EventIds.DownloadENCFilesRequestCompleted,
                "File share service download request for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with \n Href: [{FileUri}]. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    var downloadPath = Path.Combine(exchangeSetRootPath, productName.Substring(0, 2), productName, editionNumber, updateNumber);
                    var result = await DownloadBatchFiles1(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
                    return result.Item2;
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
                    return await DownloadBatchFiles(item, item.Files.Select(a => a.Links.Get.Href).ToList(), downloadPath, message, cancellationTokenSource, cancellationToken);
                }, productName, editionNumber, updateNumber, item.Files.Select(a => a.Links.Get.Href), message.BatchId, message.CorrelationId);
        }

        public bool CheckProductDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "CellName" && a.Value == product.ProductName);
        }

        public bool CheckEditionNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            return batchDetail.Attributes.Any(a => a.Key == "EditionNumber" && product.EditionNumber.Value.ToString() == a.Value);
        }

        public bool CheckUpdateNumberDoesExistInResponseItem(BatchDetail batchDetail, Products product)
        {
            var matchProduct = batchDetail.Attributes.Where(a => a.Key == "UpdateNumber");
            var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
            return product.UpdateNumbers.Any(x => x.Value.ToString() == updateNumber);
        }

        private async Task<SearchBatchResponse> SearchBatchResponse(HttpResponseMessage httpResponse)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }

        public (string, string) GenerateQueryForFss(List<Products> products)
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

        public async Task<bool> DownloadBatchFiles(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile(entry, uri, downloadPath, payloadJson, accessToken, queueMessage, cancellationTokenSource, cancellationToken);
        }

        public async Task<(bool, List<FileDetails>)> DownloadBatchFiles1(BatchDetail entry, IEnumerable<string> uri, string downloadPath, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile1(entry, uri, downloadPath, payloadJson, accessToken, queueMessage, cancellationTokenSource, cancellationToken);
        }

        private async Task<bool> ProcessBatchFile(BatchDetail entry, IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            bool result = false;
            foreach (var item in uri)
            {
                var fileName = item.Split("/").Last();
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{Uri} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item, CancellationToken.None, queueMessage.CorrelationId);

                var requestUri = new Uri(httpResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var serverValue = httpResponse.Headers.Server.ToString().Split('/').First();
                    fileSystemHelper.CheckAndCreateFolder(downloadPath);
                    string path = Path.Combine(downloadPath, fileName);
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

                var fssSearchResponseCache = new FssSearchResponseCache
                {
                    BatchId = entry.BatchId,
                    PartitionKey = productName,
                    RowKey = $"{editionNumber}|{updateNumber}",
                    Response = JsonConvert.SerializeObject(entry)
                };
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
                    "File share service search response insert/merge request in azure table for cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with FSS BatchId:{FssBatchId}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                        return result;
                    }, productName, editionNumber, updateNumber, entry.BatchId, queueMessage.BatchId, queueMessage.CorrelationId);
            }
            return result;
        }

        private async Task<(bool, List<FileDetails>)> ProcessBatchFile1(BatchDetail entry, IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken, SalesCatalogueServiceResponseQueueMessage queueMessage, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            bool result = false;
            List<FileDetails> fileList = new List<FileDetails>();
            foreach (var item in uri)
            {
                var fileName = item.Split("/").Last();
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while downloading ENC file:{fileName} from File Share Service with CancellationToken:{cancellationTokenSource.Token} with uri:{Uri} and BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileName, JsonConvert.SerializeObject(cancellationTokenSource.Token), uri, queueMessage.BatchId, queueMessage.CorrelationId);
                    throw new OperationCanceledException();
                }
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item, CancellationToken.None, queueMessage.CorrelationId);

                var requestUri = new Uri(httpResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var serverValue = httpResponse.Headers.Server.ToString().Split('/').First();
                    //fileSystemHelper.CheckAndCreateFolder(downloadPath);
                    string path = Path.Combine(downloadPath, fileName);
                    //if (!fileSystemHelper.CheckFileExists(path) || CommonHelper.IsPeriodicOutputService)
                    if (!fileList.Exists(x => x.FilePath == path))
                    {
                        //await CopyFileToFolder(httpResponse, path, fileName, entry, queueMessage);
                        var fileDetails = await CopyFileToFolder1(httpResponse, path, fileName, entry, queueMessage);
                        fileList.Add(fileDetails);
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

                var fssSearchResponseCache = new FssSearchResponseCache
                {
                    BatchId = entry.BatchId,
                    PartitionKey = productName,
                    RowKey = $"{editionNumber}|{updateNumber}",
                    Response = JsonConvert.SerializeObject(entry)
                };
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceSearchResponseStoreToCacheStart, EventIds.FileShareServiceSearchResponseStoreToCacheCompleted,
                    "File share service search response insert/merge request in azure table for cache for Product/CellName:{ProductName}, EditionNumber:{EditionNumber} and UpdateNumber:{UpdateNumber} with FSS BatchId:{FssBatchId}. BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        await fileShareServiceCache.InsertOrMergeFssCacheDetail(fssSearchResponseCache);
                        return result;
                    }, productName, editionNumber, updateNumber, entry.BatchId, queueMessage.BatchId, queueMessage.CorrelationId);
            }
            return (result, fileList);
        }

        private async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path, string fileName, BatchDetail entry, SalesCatalogueServiceResponseQueueMessage queueMessage)
        {
            byte[] bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());
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

        private async Task<FileDetails> CopyFileToFolder1(HttpResponseMessage httpResponse, string path, string fileName, BatchDetail entry, SalesCatalogueServiceResponseQueueMessage queueMessage)
        {
            byte[] bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());
            //fileSystemHelper.CreateFileCopy(path, new MemoryStream(bytes));
            var fileContent = new FileDetails
            {
                FileName = fileName,
                FilePath = path,
                FileContent = bytes
            };
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
            return fileContent;
        }
        public async Task<bool> DownloadReadMeFile(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string fileName = fileShareServiceConfig.Value.ReadMeFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
            HttpResponseMessage httpReadMeFileResponse;
            httpReadMeFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, readMeFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpReadMeFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpReadMeFileResponse.Headers.Server.ToString().Split('/').First();
                using (Stream stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadReadmeFile307RedirectResponse.ToEventId(), "File share service download readme.txt redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    return fileSystemHelper.DownloadReadmeFile(filePath, stream, lineToWrite);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadReadMeFileNonOkResponse.ToEventId(), "Error in file share service while downloading readme.txt file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpReadMeFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadReadMeFileNonOkResponse.ToEventId());
            }
        }

        public async Task<List<(string fileName, string filePath, byte[] fileContent)>> DownloadReadMeFile1(string readMeFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            string fileName = fileShareServiceConfig.Value.ReadMeFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);

            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
           
            HttpResponseMessage httpReadMeFileResponse;
            httpReadMeFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, readMeFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpReadMeFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpReadMeFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpReadMeFileResponse.Headers.Server.ToString().Split('/').First();
                using (Stream stream = await httpReadMeFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadReadmeFile307RedirectResponse.ToEventId(), "File share service download readme.txt redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    var content= fileSystemHelper.DownloadReadmeFile1(stream, lineToWrite);
                    
                    var fileContents = new List<(string fileName, string filePath, byte[] fileContent)>();
                    fileContents.Add((fileName, filePath, content));
                    return fileContents;
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadReadMeFileNonOkResponse.ToEventId(), "Error in file share service while downloading readme.txt file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpReadMeFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadReadMeFileNonOkResponse.ToEventId());
            }
        }

        public async Task<bool> DownloadIhoCrtFile(string ihoCrtFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string fileName = fileShareServiceConfig.Value.IhoCrtFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
            HttpResponseMessage httpIhoCrtFileResponse;
            httpIhoCrtFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, ihoCrtFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpIhoCrtFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpIhoCrtFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpIhoCrtFileResponse.Headers.Server.ToString().Split('/').First();
                using (Stream stream = await httpIhoCrtFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadIhoCrtFile307RedirectResponse.ToEventId(), "File share service download IHO.crt redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    return fileSystemHelper.DownloadIhoCrtFile(filePath, stream, lineToWrite);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadIhoCrtFileNonOkResponse.ToEventId(), "Error in file share service while downloading IHO.crt file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpIhoCrtFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadIhoCrtFileNonOkResponse.ToEventId());
            }
        }


        public async Task<bool> DownloadIhoPubFile(string ihoPubFilePath, string batchId, string exchangeSetRootPath, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string fileName = fileShareServiceConfig.Value.IhoPubFileName;
            string filePath = Path.Combine(exchangeSetRootPath, fileName);
            fileSystemHelper.CheckAndCreateFolder(exchangeSetRootPath);
            string lineToWrite = string.Concat("File date: ", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ssZ", CultureInfo.InvariantCulture));
            HttpResponseMessage httpIhoPubFileResponse;
            httpIhoPubFileResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, ihoPubFilePath, CancellationToken.None, correlationId);

            var requestUri = new Uri(httpIhoPubFileResponse.RequestMessage.RequestUri.ToString()).GetLeftPart(UriPartial.Path);

            if (httpIhoPubFileResponse.IsSuccessStatusCode)
            {
                var serverValue = httpIhoPubFileResponse.Headers.Server.ToString().Split('/').First();
                using (Stream stream = await httpIhoPubFileResponse.Content.ReadAsStreamAsync())
                {
                    if (serverValue == ServerHeaderValue)
                    {
                        logger.LogInformation(EventIds.DownloadIhoPubFile307RedirectResponse.ToEventId(), "File share service download IHO.pub redirected with uri:{requestUri} responded with 307 code for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", requestUri, batchId, correlationId);
                    }
                    return fileSystemHelper.DownloadFile(filePath, stream, lineToWrite);
                }
            }
            else
            {
                logger.LogError(EventIds.DownloadIhoPubFileNonOkResponse.ToEventId(), "Error in file share service while downloading IHO.pub file with uri:{requestUri} responded with {StatusCode} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} ", requestUri, httpIhoPubFileResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.DownloadIhoPubFileNonOkResponse.ToEventId());
            }
        }

        public async Task<string> SearchReadMeFilePath(string batchId, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.ReadMeFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.ReadMeTextFileNotFound.ToEventId(), "Error in file share service readme.txt not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.ReadMeTextFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId(), "Error in file share service while searching ReadMe file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceReadMeFileNonOkResponse.ToEventId());
            }

            return filePath;
        }

        public async Task<string> SearchIhoCrtFilePath(string batchId, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.IhoCrtFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.IhoCrtFileNotFound.ToEventId(), "Error in file share service IHO.crt not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.IhoCrtFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceIhoCrtFileNonOkResponse.ToEventId(), "Error in file share service while searching IHO.crt file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceIhoCrtFileNonOkResponse.ToEventId());
            }

            return filePath;
        }

        public async Task<string> SearchIhoPubFilePath(string batchId, string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            string filePath = string.Empty;
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq '{fileShareServiceConfig.Value.IhoPubFileName}' and BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, CancellationToken.None, correlationId);
            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.FirstOrDefault();
                    filePath = batchResult.Files.FirstOrDefault()?.Links.Get.Href;
                }
                else
                {
                    logger.LogError(EventIds.IhoPubFileNotFound.ToEventId(), "Error in file share service IHO.pub not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.IhoPubFileNotFound.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceIhoPubFileNonOkResponse.ToEventId(), "Error in file share service while searching IHO.pub file with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceIhoPubFileNonOkResponse.ToEventId());
            }

            return filePath;
        }

        public async Task<bool> CreateZipFileForExchangeSet(string batchId, string exchangeSetZipRootPath, string correlationId)
        {
            bool isCreateZipFileExchangeSetCreated = false;
            var zipName = $"{exchangeSetZipRootPath}.zip";
            string filePath = Path.Combine(exchangeSetZipRootPath, zipName);
            if (fileSystemHelper.CheckDirectoryAndFileExists(exchangeSetZipRootPath, filePath))
            {
                fileSystemHelper.CreateZipFile(exchangeSetZipRootPath, zipName);
                await Task.CompletedTask;

                if (fileSystemHelper.CheckFileExists(zipName))
                {
                    logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Exchange set zip:{ExchangeSetFileName} created for BatchId:{BatchId} and  _X-Correlation-ID:{correlationId}", fileSystemHelper.GetFileName(zipName), batchId, correlationId);
                    isCreateZipFileExchangeSetCreated = true;
                }
                else
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileSystemHelper.GetFileName(zipName), batchId, correlationId);
                    throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
                }
            }
            else
            {
                logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                throw new FulfilmentException(EventIds.ErrorInCreatingZipFile.ToEventId());
            }
            return isCreateZipFileExchangeSetCreated;
        }

        //Upload either Exchange Set or Error File
        public async Task<bool> UploadFileToFileShareService(string batchId, string exchangeSetZipRootPath, string correlationId, string fileName)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            bool isUploadZipFile = false;
            DateTime uploadZipFileTaskStartedAt = DateTime.UtcNow;
            CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipRootPath, fileName));

            var fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = customFileInfo.Name,
                Length = customFileInfo.Length
            };
            bool isZipFileCreated = await CreateFile(fileCreateMetaData, accessToken, correlationId);
            if (isZipFileCreated)
            {
                bool isWriteBlock = await UploadAndWriteBlock(batchId, correlationId, accessToken, customFileInfo);
                if (isWriteBlock)
                {
                    isUploadZipFile = true;
                    DateTime uploadZipFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Upload Zip File Task", uploadZipFileTaskStartedAt, uploadZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
                }
            }
            return isUploadZipFile;
        }

        //Commit and check batch status of ENC / AIO / Error.txt batch to FSS
        public async Task<bool> CommitBatchToFss(string batchId, string correlationId, string exchangeSetZipPath, string fileName = ZIPFILE)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            bool isBatchCommitted = false;
            var batchCommitMetaDataList = new List<BatchCommitMetaData>();

            if (fileName == ZIPFILE)
            {
                //Get zip files full path
                string[] exchangeSetZipList = fileSystemHelper.GetFiles(exchangeSetZipPath).Where(di => di.Contains(ZIPFILE)).ToArray();

                foreach (var zipFileName in exchangeSetZipList)
                {
                    CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(zipFileName);

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
                CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipPath, fileName));

                var batchCommitMetaData = new BatchCommitMetaData()
                {
                    BatchId = batchId,
                    AccessToken = accessToken,
                    FileName = customFileInfo.Name,
                    FullFileName = customFileInfo.FullName
                };

                batchCommitMetaDataList.Add(batchCommitMetaData);
            }

            BatchStatus batchStatus = await CommitAndGetBatchStatus(batchId, correlationId, accessToken, batchCommitMetaDataList);
            if (batchStatus == BatchStatus.Committed)
            {
                isBatchCommitted = true;
            }
            logger.LogInformation(EventIds.BatchStatus.ToEventId(), "BatchStatus:{batchStatus} for file:{fileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, GetFileNameOfCommittedBatch(batchCommitMetaDataList), batchId, correlationId);
            return isBatchCommitted;
        }

        private async Task<BatchStatus> CommitAndGetBatchStatus(string batchId, string correlationId, string accessToken, List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            DateTime commitTaskStartedAt = DateTime.UtcNow;
            bool isUploadCommitBatchCompleted = await UploadCommitBatch(batchCommitMetaDataList, correlationId);
            BatchStatus batchStatus = BatchStatus.CommitInProgress;
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
            DateTime commitTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Commit Batch Task", commitTaskStartedAt, commitTaskCompletedAt, correlationId, null, null, null, batchId);
            return batchStatus;
        }

        public async Task<bool> UploadAndWriteBlock(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            var blockIdList = await UploadBlockFile(batchId, customFileInfo, accessToken, correlationId);
            var writeBlocksToFileMetaData = new WriteBlocksToFileMetaData()
            {
                BatchId = batchId,
                FileName = customFileInfo.Name,
                AccessToken = accessToken,
                BlockIds = blockIdList
            };
            return await WriteBlockFile(writeBlocksToFileMetaData, correlationId);
        }

        public Task<bool> CreateFile(FileCreateMetaData fileCreateMetaData, string accessToken, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateFileInBatchStart, EventIds.CreateFileInBatchCompleted,
                    "File:{FileName} creation in batch for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        HttpResponseMessage httpResponse;

                        string mimetype = fileCreateMetaData.FileName.Contains("zip") ? "application/zip" : "text/plain";

                        httpResponse = await fileShareServiceClient.AddFileInBatchAsync(HttpMethod.Post, new FileCreateModel(), accessToken, fileShareServiceConfig.Value.BaseUrl, fileCreateMetaData.BatchId, fileCreateMetaData.FileName, fileCreateMetaData.Length, mimetype, correlationId);
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            logger.LogError(EventIds.CreateFileInBatchNonOkResponse.ToEventId(), "Error while creating/adding file:{FileName} in batch with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fileCreateMetaData.FileName, httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, fileCreateMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.CreateFileInBatchNonOkResponse.ToEventId());
                        }
                        return true;
                    }, fileCreateMetaData.FileName, fileCreateMetaData.BatchId, correlationId);
        }

        public async Task<List<string>> UploadBlockFile(string batchId, CustomFileInfo customFileInfo, string accessToken, string correlationId)
        {
            var uploadMessage = new UploadMessage()
            {
                UploadSize = customFileInfo.Length,
                BlockSizeInMultipleOfKBs = fileShareServiceConfig.Value.BlockSizeInMultipleOfKBs
            };
            long blockSizeInMultipleOfKBs = uploadMessage.BlockSizeInMultipleOfKBs <= 0 || uploadMessage.BlockSizeInMultipleOfKBs > 4096
                ? 1024
                : uploadMessage.BlockSizeInMultipleOfKBs;
            long blockSize = blockSizeInMultipleOfKBs * 1024;
            var blockIdList = new List<string>();
            var ParallelBlockUploadTasks = new List<Task>();
            long uploadedBytes = 0;
            int blockNum = 0;
            using var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            while (uploadedBytes < customFileInfo.Length)
            {
                blockNum++;
                int readBlockSize = (int)(customFileInfo.Length - uploadedBytes <= blockSize ? customFileInfo.Length - uploadedBytes : blockSize);
                string blockId = CommonHelper.GetBlockIds(blockNum);
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while uploading blocks in File Share Service with CancellationToken:{cancellationToken.Source} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), blockId, customFileInfo.Name, batchId, correlationId);
                    throw new OperationCanceledException();
                }
                var blockUploadMetaData = new UploadBlockMetaData()
                {
                    BatchId = batchId,
                    BlockId = blockId,
                    FullFileName = customFileInfo.FullName,
                    JwtToken = accessToken,
                    Offset = uploadedBytes,
                    Length = readBlockSize,
                    FileName = customFileInfo.Name
                };
                ParallelBlockUploadTasks.Add(UploadFileBlockMetaData(blockUploadMetaData, correlationId, cancellationTokenSource, cancellationToken));
                blockIdList.Add(blockId);
                uploadedBytes += readBlockSize;
                //run uploads in parallel	
                if (ParallelBlockUploadTasks.Count >= fileShareServiceConfig.Value.ParallelUploadThreadCount)
                {
                    Task.WaitAll(ParallelBlockUploadTasks.ToArray());
                    ParallelBlockUploadTasks.Clear();
                }
            }
            if (ParallelBlockUploadTasks.Count > 0)
            {
                await Task.WhenAll(ParallelBlockUploadTasks);
                ParallelBlockUploadTasks.Clear();
            }
            return blockIdList;
        }

        public Task UploadFileBlockMetaData(UploadBlockMetaData UploadBlockMetaData, string correlationId, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            return logger.LogStartEndAndElapsedTime(EventIds.UploadFileBlockStarted, EventIds.UploadFileBlockCompleted,
                    "UploadFileBlock for BlockId:{BlockId} and file:{FileName} and Batch:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        byte[] byteData = fileSystemHelper.UploadFileBlockMetaData(UploadBlockMetaData);
                        var blockMd5Hash = CommonHelper.CalculateMD5(byteData);
                        HttpResponseMessage httpResponse;
                        httpResponse = await fileShareServiceClient.UploadFileBlockAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, UploadBlockMetaData.BatchId, UploadBlockMetaData.FileName, UploadBlockMetaData.BlockId, byteData, blockMd5Hash, UploadBlockMetaData.JwtToken, cancellationToken, correlationId);

                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            cancellationTokenSource.Cancel();
                            logger.LogError(EventIds.UploadFileBlockNonOkResponse.ToEventId(), "Error in uploading file blocks with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
                            logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Request cancelled for Error in uploading file blocks with CancellationToken:{ cancellationTokenSource.Token} with uri {RequestUri} responded with {StatusCode} for BlockId:{BlockId} and file:{FileName} and BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.UploadFileBlockNonOkResponse.ToEventId());
                        }
                    }, UploadBlockMetaData.BlockId, UploadBlockMetaData.FileName, UploadBlockMetaData.BatchId, correlationId);
        }

        public Task<bool> WriteBlockFile(WriteBlocksToFileMetaData writeBlocksToFileMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.WriteBlocksToFileStart, EventIds.WriteBlockToFileNonOkResponse,
                    "Write Blocks to file:{FileName} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () =>
                    {
                        var writeBlockfileModel = new WriteBlockFileModel()
                        {
                            BlockIds = writeBlocksToFileMetaData.BlockIds
                        };
                        HttpResponseMessage httpResponse;
                        httpResponse = await fileShareServiceClient.WriteBlockInFileAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, writeBlocksToFileMetaData.BatchId, writeBlocksToFileMetaData.FileName, writeBlockfileModel, writeBlocksToFileMetaData.AccessToken, correlationId: correlationId);
                        if (!httpResponse.IsSuccessStatusCode)
                        {
                            logger.LogError(EventIds.WriteBlockToFileNonOkResponse.ToEventId(), "Error in writing Blocks with uri:{RequestUri} responded with {StatusCode} for file:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, writeBlocksToFileMetaData.FileName, writeBlocksToFileMetaData.BatchId, correlationId);
                            throw new FulfilmentException(EventIds.WriteBlockToFileNonOkResponse.ToEventId());
                        }
                        return true;
                    }, writeBlocksToFileMetaData.FileName, writeBlocksToFileMetaData.BatchId, correlationId);
        }

        public Task<bool> UploadCommitBatch(List<BatchCommitMetaData> batchCommitMetaDataList, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadCommitBatchStart, EventIds.UploadCommitBatchCompleted,
                "Upload Commit Batch for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    List<FileDetail> fileDetails = fileSystemHelper.UploadLargeMediaCommitBatch(batchCommitMetaDataList);
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

        private static string GetFileNameOfCommittedBatch(List<BatchCommitMetaData> batchCommitMetaDataList)
        {
            return batchCommitMetaDataList.ElementAtOrDefault(1) == null ? batchCommitMetaDataList[0].FileName :
                            batchCommitMetaDataList[0].FileName + " and " + batchCommitMetaDataList[1].FileName;
        }

        public Task<BatchStatus> GetBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
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

        #region LargeMediaExchangeSet 

        public async Task<bool> UploadLargeMediaFileToFileShareService(string batchId, string exchangeSetZipPath, string correlationId, string fileName)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            bool isWriteBlock = false;
            DateTime uploadZipFileTaskStartedAt = DateTime.UtcNow;
            CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(Path.Combine(exchangeSetZipPath, fileName));

            var fileCreateMetaData = new FileCreateMetaData()
            {
                AccessToken = accessToken,
                BatchId = batchId,
                FileName = customFileInfo.Name,
                Length = customFileInfo.Length
            };
            bool isZipFileCreated = await CreateFile(fileCreateMetaData, accessToken, correlationId);
            if (isZipFileCreated)
            {
                isWriteBlock = await UploadAndWriteBlock(batchId, correlationId, accessToken, customFileInfo);
                if (isWriteBlock)
                {
                    DateTime uploadZipFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Upload Zip File Task", uploadZipFileTaskStartedAt, uploadZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
                }
            }
            return isWriteBlock;
        }

        public async Task<bool> CommitAndGetBatchStatusForLargeMediaExchangeSet(string batchId, string exchangeSetZipPath, string correlationId)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            var batchCommitMetaDataList = new List<BatchCommitMetaData>();

            //Get zip files full path
            string[] mediaZipList = fileSystemHelper.GetFiles(exchangeSetZipPath).Where(di => di.Contains("zip")).ToArray();

            foreach (var fileName in mediaZipList)
            {
                CustomFileInfo customFileInfo = fileSystemHelper.GetFileInfo(fileName);

                var batchCommitMetaData = new BatchCommitMetaData()
                {
                    BatchId = batchId,
                    AccessToken = accessToken,
                    FileName = customFileInfo.Name,
                    FullFileName = customFileInfo.FullName
                };

                batchCommitMetaDataList.Add(batchCommitMetaData);
            }

            DateTime commitTaskStartedAt = DateTime.UtcNow;
            bool isUploadCommitBatchCompleted = await UploadCommitBatchForLargeMediaExchangeSet(batchCommitMetaDataList, correlationId);
            BatchStatus batchStatus = BatchStatus.CommitInProgress;
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
            DateTime commitTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Commit Batch Task", commitTaskStartedAt, commitTaskCompletedAt, correlationId, null, null, null, batchId);

            logger.LogInformation(EventIds.BatchStatus.ToEventId(), "BatchStatus:{batchStatus} for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, batchId, correlationId);

            return batchStatus == BatchStatus.Committed;
        }

        private Task<bool> UploadCommitBatchForLargeMediaExchangeSet(List<BatchCommitMetaData> batchCommitMetaDataList, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadCommitBatchStart, EventIds.UploadCommitBatchCompleted,
                "Upload Commit Batch for large media exchange set of BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    List<FileDetail> fileDetails = fileSystemHelper.UploadLargeMediaCommitBatch(batchCommitMetaDataList);
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
                    string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                    ResponseBatchStatusModel responseBatchStatusModel = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(bodyJson);
                    return (BatchStatus)Enum.Parse(typeof(BatchStatus), responseBatchStatusModel.Status.ToString());
                }, batchStatusMetaData.BatchId, correlationId);
        }

        // This function is used to search Info and Adc folder details from FSS for large exchange set
        public async Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string uri)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, null, accessToken, uri, CancellationToken.None, correlationId);

            IEnumerable<BatchFile> fileDetails = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
                if (searchBatchResponse.Entries.Count > 0)
                {
                    var batchResult = searchBatchResponse.Entries.OrderByDescending(j => j.BatchPublishedDate).FirstOrDefault();
                    fileDetails = batchResult?.Files.Select(x => new BatchFile
                    {
                        Filename = x.Filename,
                        Links = new Links
                        {
                            Get = new Link
                            {
                                Href = x.Links.Get.Href
                            }
                        }
                    });
                }
                else
                {
                    logger.LogError(EventIds.SearchFolderFilesNotFound.ToEventId(), "Error in file share service, folder files not found for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
                    throw new FulfilmentException(EventIds.SearchFolderFilesNotFound.ToEventId());
                }
                logger.LogInformation(EventIds.QueryFileShareServiceSearchFolderFileOkResponse.ToEventId(), "Successfully searched files path for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchId, correlationId);
            }
            else
            {
                logger.LogError(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId(), "Error in file share service while searching folder files with uri {RequestUri} responded with {StatusCode} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchId, correlationId);
                throw new FulfilmentException(EventIds.QueryFileShareServiceSearchFolderFileNonOkResponse.ToEventId());
            }
            return fileDetails;
        }

        // This function is used to download Info and Adc folder details from FSS for large exchange set
        public async Task<bool> DownloadFolderDetails(string batchId, string correlationId, IEnumerable<BatchFile> fileDetails, string exchangeSetPath)
        {
            var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            foreach (var item in fileDetails)
            {
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, null, accessToken, item.Links.Get.Href, CancellationToken.None, correlationId);

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

        #endregion
    }
}