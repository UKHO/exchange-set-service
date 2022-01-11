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
        private const string ServerHeaderValue = "Windows-Azure-Blob";

        public FileShareService(IFileShareServiceClient fileShareServiceClient,
                                IAuthFssTokenProvider authFssTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                ILogger<FileShareService> logger,
                                IFileShareServiceCache fileShareServiceCache,
                                IOptions<CacheConfiguration> fssCacheConfiguration,
                                IFileSystemHelper fileSystemHelper, IMonitorHelper monitorHelper)
        {
            this.fileShareServiceClient = fileShareServiceClient;
            this.authFssTokenProvider = authFssTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;
            this.fileShareServiceCache = fileShareServiceCache;
            this.fssCacheConfiguration = fssCacheConfiguration;
            this.fileSystemHelper = fileSystemHelper;
            this.monitorHelper = monitorHelper;
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
            CreateBatchRequest createBatchRequest = new CreateBatchRequest
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
            }

            return createBatchResponse;
        }

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
            SearchBatchResponse internalSearchBatchResponse = new SearchBatchResponse 
            {
                Entries = new List<BatchDetail>()
            };
            List<Products> cacheProductsNotFound = fssCacheConfiguration.Value.IsFssCacheEnabled ? await fileShareServiceCache.GetNonCachedProductDataForFss(products, internalSearchBatchResponse, exchangeSetRootPath, message, cancellationTokenSource, cancellationToken) : products;
            if (cacheProductsNotFound != null && cacheProductsNotFound.Any())
            {
                List<Products> internalNotFoundProducts = new List<Products>();
                var accessToken = await authFssTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
                var productWithAttributes = GenerateQueryForFss(cacheProductsNotFound);
                var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter={fileShareServiceConfig.Value.ProductCode} {productWithAttributes.Item1}";

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
                            uri = await SelectLatestPublishedDateBatch(cacheProductsNotFound, internalSearchBatchResponse, uri, httpResponse, productList, message, cancellationTokenSource, cancellationToken, exchangeSetRootPath);
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

        private void CheckProductsExistsInFileShareService(List<Products> products, string correlationId, string batchId, SearchBatchResponse internalSearchBatchResponse, List<Products> internalNotFoundProducts, int prodCount, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.LogError(EventIds.CancellationTokenEvent.ToEventId(), "Operation cancelled as IsCancellationRequested flag is true while searching ENC files and no data found while querying with CancellationToken:{cancellationTokenSource.Token} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(cancellationTokenSource.Token), batchId, correlationId);
                throw new OperationCanceledException();
            }

            if (internalSearchBatchResponse.Entries.Any() && prodCount != internalSearchBatchResponse.Entries.Count)
            {
                List<Products> internalProducts = new List<Products>();
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
                            UpdateNumbers = new List<int?> { itemUpdateNumber }
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

        private async Task<string> SelectLatestPublishedDateBatch(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string uri, HttpResponseMessage httpResponse, List<string> productList, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
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
                uri = searchBatchResponse.Links.Next?.Href;
            }

            return uri;
        }

        private async Task CheckProductOrCancellationData(SearchBatchResponse internalSearchBatchResponse, List<string> productList, BatchDetail item, Products productItem, string updateNumber, string compareProducts, SalesCatalogueServiceResponseQueueMessage message, CancellationTokenSource cancellationTokenSource, CancellationToken cancellationToken, string exchangeSetRootPath)
        {
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
            var productIndex = 1;
            var productCount = products.Count;
            var sb = new StringBuilder();
            var sbLog = new StringBuilder();
            if (products != null && products.Any())
            {
                sb.Append("(");////1st main (
                foreach (var item in products)
                {
                    var itemSb = new StringBuilder();
                    var cancellation = new StringBuilder();
                    itemSb.Append("(");////1st product
                    itemSb.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                    itemSb.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.EditionNumber);
                    var lstCount = item.UpdateNumbers.Count;
                    var index = 1;
                    if (item.UpdateNumbers != null && item.UpdateNumbers.Any())
                    {
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
                    }
                    itemSb.Append(cancellation.ToString() + (productCount == productIndex ? ")" : ") or "));/////last product or with multiple
                    sbLog.AppendFormat("\n Product/CellName:{0}, EditionNumber:{1} and UpdateNumbers:[{2}]", item.ProductName, item.EditionNumber.ToString(), string.Join(",", item?.UpdateNumbers.Select(a => a.Value.ToString())));
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
                    if (!fileSystemHelper.CheckFileExists(path))
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

        private async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path, string fileName, BatchDetail entry, SalesCatalogueServiceResponseQueueMessage queueMessage)
        {
            byte[] bytes = fileSystemHelper.ConvertStreamToByteArray(await httpResponse.Content.ReadAsStreamAsync());
            fileSystemHelper.CreateFileCopy(path, new MemoryStream(bytes));
            if (!entry.IgnoreCache && fssCacheConfiguration.Value.IsFssCacheEnabled)
            {
                await logger.LogStartEndAndElapsedTimeAsync(EventIds.FileShareServiceUploadENCFilesToCacheStart, EventIds.FileShareServiceUploadENCFilesToCacheCompleted,
                    "File share service upload ENC file request to cache blob container for Container:{Container}, with FileName: {FileName}. ESS BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                    async () => {
                        await fileShareServiceCache.CopyFileToBlob(new MemoryStream(bytes), fileName, entry.BatchId);
                        return Task.CompletedTask;
                    }, entry.BatchId, fileName, queueMessage.BatchId, queueMessage.CorrelationId);
            }
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
                    filePath = batchResult.Files.FirstOrDefault().Links.Get.Href;
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
                    logger.LogInformation(EventIds.CreateZipFileRequestCompleted.ToEventId(), "Exchange set zip:{ExchangeSetFileName} created for BatchId:{BatchId} and  _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
                    isCreateZipFileExchangeSetCreated = true;
                }
                else
                {
                    logger.LogError(EventIds.ErrorInCreatingZipFile.ToEventId(), "Error in creating exchange set zip:{ExchangeSetFileName} for BatchId:{BatchId} and _X-Correlation-ID:{correlationId}", fileShareServiceConfig.Value.ExchangeSetFileName, batchId, correlationId);
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

            FileCreateMetaData fileCreateMetaData = new FileCreateMetaData()
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
                    DateTime uploadZipFileTaskCompletedAt = DateTime.UtcNow;
                    monitorHelper.MonitorRequest("Upload Zip File Task", uploadZipFileTaskStartedAt, uploadZipFileTaskCompletedAt, correlationId, null, null, null, batchId);
                    BatchStatus batchStatus = await CommitAndGetBatchStatus(batchId, correlationId, accessToken, customFileInfo);
                    if (batchStatus == BatchStatus.Committed)
                    {
                        isUploadZipFile = true;
                    }
                    logger.LogInformation(EventIds.BatchStatus.ToEventId(), "BatchStatus:{batchStatus} for file:{fileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, fileName, batchId, correlationId);
                }
            }
            return isUploadZipFile;
        }

        public async Task<BatchStatus> CommitAndGetBatchStatus(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            BatchCommitMetaData batchCommitMetaData = new BatchCommitMetaData()
            {
                BatchId = batchId,
                AccessToken = accessToken,
                FileName = customFileInfo.Name,
                FullFileName = customFileInfo.FullName
            };
            DateTime commitTaskStartedAt = DateTime.UtcNow;
            bool isUploadCommitBatchCompleted = await UploadCommitBatch(batchCommitMetaData, correlationId);
            BatchStatus batchStatus = BatchStatus.CommitInProgress;
            if (isUploadCommitBatchCompleted)
            {
                var batchStatusMetaData = new BatchStatusMetaData()
                {
                    AccessToken = accessToken,
                    BatchId = batchId,
                    FileName = customFileInfo.Name
                };
                Stopwatch watch = new Stopwatch();
                watch.Start();
                while (batchStatus != BatchStatus.Committed && watch.Elapsed.TotalMinutes <= fileShareServiceConfig.Value.BatchCommitCutOffTimeInMinutes)
                {
                    batchStatus = await GetBatchStatus(batchStatusMetaData, correlationId);
                    if (batchStatus == BatchStatus.Failed)
                    {
                        watch.Stop();
                        logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for file:{Name} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", customFileInfo.Name, batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());

                    }
                    await Task.Delay(fileShareServiceConfig.Value.BatchCommitDelayTimeInMilliseconds);
                }
                if (batchStatus != BatchStatus.Committed)
                {
                    watch.Stop();
                    logger.LogError(EventIds.BatchCommitTimeout.ToEventId(), "Batch Commit Status timeout with BatchStatus:{batchStatus} for file:{Name} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", batchStatus, customFileInfo.Name, batchStatusMetaData.BatchId, correlationId);
                    throw new FulfilmentException(EventIds.BatchCommitTimeout.ToEventId());
                }
                watch.Stop();
            }
            else
            {
                logger.LogError(EventIds.BatchFailedStatus.ToEventId(), "Batch status failed for file:{Name} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", customFileInfo.Name, batchId, correlationId);
                throw new FulfilmentException(EventIds.BatchFailedStatus.ToEventId());
            }
            DateTime commitTaskCompletedAt = DateTime.UtcNow;
            monitorHelper.MonitorRequest("Commit Batch Task", commitTaskStartedAt, commitTaskCompletedAt, correlationId, null, null, null, batchId);
            return batchStatus;
        }

        public async Task<bool> UploadAndWriteBlock(string batchId, string correlationId, string accessToken, CustomFileInfo customFileInfo)
        {
            var blockIdList = await UploadBlockFile(batchId, customFileInfo, accessToken, correlationId);
            WriteBlocksToFileMetaData writeBlocksToFileMetaData = new WriteBlocksToFileMetaData()
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
                    async () => {
                        HttpResponseMessage httpResponse;

                        string mimetype = fileCreateMetaData.FileName == fileShareServiceConfig.Value.ExchangeSetFileName ? "application/zip" : "text/plain";

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
            UploadMessage uploadMessage = new UploadMessage()
            {
                UploadSize = customFileInfo.Length,
                BlockSizeInMultipleOfKBs = fileShareServiceConfig.Value.BlockSizeInMultipleOfKBs
            };
            var blockSizeInMultipleOfKBs = uploadMessage.BlockSizeInMultipleOfKBs <= 0
                                            || uploadMessage.BlockSizeInMultipleOfKBs > 4096 ? 1024 : uploadMessage.BlockSizeInMultipleOfKBs;
            long blockSize = blockSizeInMultipleOfKBs * 1024;
            List<string> blockIdList = new List<string>();
            List<Task> ParallelBlockUploadTasks = new List<Task>();
            long uploadedBytes = 0;
            int blockNum = 0;
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
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
                    async () => {
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
                    async () => {
                        WriteBlockFileModel writeBlockfileModel = new WriteBlockFileModel()
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

        public Task<bool> UploadCommitBatch(BatchCommitMetaData batchCommitMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.UploadCommitBatchStart, EventIds.UploadCommitBatchCompleted,
                "Upload Commit Batch for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    List<FileDetail> fileDetails = fileSystemHelper.UploadCommitBatch(batchCommitMetaData);
                    var batchCommitModel = new BatchCommitModel()
                    {
                        FileDetails = fileDetails
                    };

                    HttpResponseMessage httpResponse;
                    httpResponse = await fileShareServiceClient.CommitBatchAsync(HttpMethod.Put, fileShareServiceConfig.Value.BaseUrl, batchCommitMetaData.BatchId, batchCommitModel, batchCommitMetaData.AccessToken, correlationId);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.UploadCommitBatchNonOkResponse.ToEventId(), "Error in Upload Commit Batch with uri:{RequestUri} responded with {StatusCode} for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchCommitMetaData.FileName, batchCommitMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.UploadCommitBatchNonOkResponse.ToEventId());
                    }
                    return true;
                }, batchCommitMetaData.FileName, batchCommitMetaData.BatchId, correlationId);
        }

        public Task<BatchStatus> GetBatchStatus(BatchStatusMetaData batchStatusMetaData, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.GetBatchStatusStart, EventIds.GetBatchStatusCompleted,
                "Get Batch Status for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    HttpResponseMessage httpResponse;
                    httpResponse = await fileShareServiceClient.GetBatchStatusAsync(HttpMethod.Get, fileShareServiceConfig.Value.BaseUrl, batchStatusMetaData.BatchId, batchStatusMetaData.AccessToken);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        logger.LogError(EventIds.GetBatchStatusNonOkResponse.ToEventId(), "Error in Get Batch Status with uri:{RequestUri} responded with {StatusCode} for FileName:{FileName} and BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, batchStatusMetaData.FileName, batchStatusMetaData.BatchId, correlationId);
                        throw new FulfilmentException(EventIds.GetBatchStatusNonOkResponse.ToEventId());
                    }
                    string bodyJson = await httpResponse.Content.ReadAsStringAsync();
                    ResponseBatchStatusModel responseBatchStatusModel = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(bodyJson);
                    return (BatchStatus)Enum.Parse(typeof(BatchStatus), responseBatchStatusModel.Status.ToString());
                }, batchStatusMetaData.FileName, batchStatusMetaData.BatchId, correlationId);            
        }
    }
}