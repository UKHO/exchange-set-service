using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService : IFileShareService
    {
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly ILogger<FileShareService> logger;

        public FileShareService(IFileShareServiceClient fileShareServiceClient,
                                IAuthTokenProvider authTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                ILogger<FileShareService> logger)
        {
            this.fileShareServiceClient = fileShareServiceClient;
            this.authTokenProvider = authTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.logger = logger;            
        }
        public async Task<CreateBatchResponse> CreateBatch()
        {
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);

            var jwtSecurityToken = new JwtSecurityToken(accessToken);
            var oid = jwtSecurityToken.Claims.FirstOrDefault(m => m.Type == "oid").Value;
            var uri = $"/batch";

            CreateBatchRequest createBatchRequest = CreateBatchRequest(oid);

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            CreateBatchResponse createBatchResponse = await CreateBatchResponse(httpResponse, createBatchRequest.ExpiryDate);
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
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Acl = new Acl()
                {
                    ReadUsers = new List<string>() { oid }
                }
            };

            return createBatchRequest;
        }

        private async Task<CreateBatchResponse> CreateBatchResponse(HttpResponseMessage httpResponse,string batchExpiryDateTime)
        {
            var createBatchResponse = new CreateBatchResponse();
            var body = await httpResponse.Content.ReadAsStringAsync();

            if (httpResponse.StatusCode != HttpStatusCode.Created)
            {
                logger.LogError(EventIds.FSSCreateBatchNonOkResponse.ToEventId(), $"File share service create batch endpoint responded with {httpResponse.StatusCode} and message {body}");
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = null;
            }
            else
            {
                createBatchResponse.ResponseCode = httpResponse.StatusCode;
                createBatchResponse.ResponseBody = JsonConvert.DeserializeObject<CreateBatchResponseModel>(body);
                createBatchResponse.ResponseBody.BatchStatusUri =$"{fileShareServiceConfig.Value.BaseUrl}/batch/{createBatchResponse.ResponseBody.BatchId}";
                createBatchResponse.ResponseBody.BatchExpiryDateTime = batchExpiryDateTime;
                createBatchResponse.ResponseBody.ExchangeSetFileUri =$"{createBatchResponse.ResponseBody.BatchStatusUri}/files/{fileShareServiceConfig.Value.ExchangeSetFileName}";
            }

            return createBatchResponse;
        }

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, string correlationId)
        {
            SearchBatchResponse internalSearchBatchResponse = new SearchBatchResponse();
            internalSearchBatchResponse.Entries = new List<BatchDetail>();
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);            
            var productWithAttributes = GenerateQueryForFss(products);
            var uri = $"/batch?limit={fileShareServiceConfig.Value.Limit}&start={fileShareServiceConfig.Value.Start}&$filter={fileShareServiceConfig.Value.ProductCode} {productWithAttributes}";

            HttpResponseMessage httpResponse;

            string payloadJson = string.Empty;
            var productList = new List<string>();
            var prodCount = products.Select(a => a.UpdateNumbers).Sum(a => a.Count);
            do
            {
                httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri, correlationId);

                if (httpResponse.IsSuccessStatusCode)
                {
                    uri = await SelectLatestPublishedDateBatch(products, internalSearchBatchResponse, uri, httpResponse, productList);
                }
                else
                {
                    logger.LogInformation(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "File share service with uri {RequestUri}, responded with {StatusCode} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                }
            } while (httpResponse.IsSuccessStatusCode && internalSearchBatchResponse.Entries.Count != 0 && internalSearchBatchResponse.Entries.Count < prodCount);

            return internalSearchBatchResponse;
        }

        private async Task<string> SelectLatestPublishedDateBatch(List<Products> products, SearchBatchResponse internalSearchBatchResponse, string uri, HttpResponseMessage httpResponse, List<string> productList)
        {
            SearchBatchResponse searchBatchResponse = await SearchBatchResponse(httpResponse);
            foreach (var item in searchBatchResponse.Entries)
            {
                foreach (var productItem in products)
                {
                    if (CheckProductDoesExistInResponseItem(item, productItem) && CheckEditionNumberDoesExistInResponseItem(item, productItem)
                        && CheckUpdateNumberDoesExistInResponseItem(item, productItem))
                    {
                        var matchProduct = item.Attributes.Where(a => a.Key == "UpdateNumber");
                        var updateNumber = matchProduct.Select(a => a.Value).FirstOrDefault();
                        var compareProducts = $"{productItem.ProductName}|{productItem.EditionNumber}|{updateNumber}";
                        if (!productList.Contains(compareProducts))
                        {
                            internalSearchBatchResponse.Entries.Add(item);
                            productList.Add(compareProducts);
                        }
                    }
                }
                uri = searchBatchResponse.Links.Next?.Href;
            }

            return uri;
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
            return product.UpdateNumbers.Where(x => x.Value.ToString() == updateNumber).ToList().Count > 0;
        }

        private async Task<SearchBatchResponse> SearchBatchResponse(HttpResponseMessage httpResponse)
        {
            var body = await httpResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchBatchResponse>(body);
        }

        public string GenerateQueryForFss(List<Products> products)
        {
            var productIndex = 1;
            var productCount = products.Count;
            StringBuilder sb = new StringBuilder();
            sb.Append("(");////1st main (
            foreach (var item in products)
            {
                sb.Append("(");////1st product
                sb.AppendFormat(fileShareServiceConfig.Value.CellName, item.ProductName);
                sb.AppendFormat(fileShareServiceConfig.Value.EditionNumber, item.EditionNumber);
                var lstCount = item.UpdateNumbers.Count;
                var index = 1;
                if (item.UpdateNumbers != null && item.UpdateNumbers.Any())
                {
                    foreach (var updateNumberItem in item.UpdateNumbers)
                    {
                        if (index == 1)
                        {
                            sb.Append("((");
                        }
                        sb.AppendFormat(fileShareServiceConfig.Value.UpdateNumber, updateNumberItem.Value);
                        sb.Append(lstCount != index ? "or " : "))");
                        index += 1;
                    }
                }
                sb.Append(productCount == productIndex ? ")" : ") or ");/////last product or with multiple
                productIndex += 1;
            }
            sb.Append(")");//// last main )
            return sb.ToString();
        }

        public async Task<bool> DownloadBatchFiles(IEnumerable<string> uri, string downloadPath,string correlationId)
        {
            string payloadJson = string.Empty;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            return await ProcessBatchFile(uri, downloadPath, payloadJson, accessToken, correlationId);
        }

        private async Task<bool> ProcessBatchFile(IEnumerable<string> uri, string downloadPath, string payloadJson, string accessToken,string correlationId)
        {
            bool result = false;
            foreach (var item in uri)
            {
                HttpResponseMessage httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, item);
                var fileName = item.Split("/").Last();
                if (httpResponse.IsSuccessStatusCode)
                {
                    CheckCreateFolderPath(downloadPath);
                    string path = Path.Combine(downloadPath, fileName);
                    if (!File.Exists(path))
                    {
                        await CopyFileToFolder(httpResponse, path);
                        result = true;
                    }
                }
                else
                {
                    logger.LogInformation(EventIds.DownloadFileShareServiceNonOkResponse.ToEventId(), "File share service download end point with uri {RequestUri} responded with {StatusCode} and _X-Correlation-ID:{correlationId}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode, correlationId);
                }
            }
            return result;
        }

        private static async Task CopyFileToFolder(HttpResponseMessage httpResponse, string path)
        {
            Stream stream = await httpResponse.Content.ReadAsStreamAsync();
            FileStream outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite); 
            stream.CopyTo(outputFileStream);
        }

        private static void CheckCreateFolderPath(string downloadPath)
        {
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
        }
    }
}
