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
            var accessToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyIsImtpZCI6Im5PbzNaRHJPRFhFSzFqS1doWHNsSFJfS1hFZyJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjIzMzMzODAzLCJuYmYiOjE2MjMzMzM4MDMsImV4cCI6MTYyMzMzNzcwMywiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQVZkME9PVHg3Sm4yMWZCTVZlTE1CNTRhL0JoaHJOOXJweXhqWklyNUIvUEI1a0dXbzNXeVlrR2V2RXNpT3pkU3gxb25JNFhYR3ZCeW5veC9rUmNtWTB3TWJWck5pYXBieStGME8vakp5dEp5QTVtdmZDZGpNWVJMY2hqdmpCZ1lIOWJYNzl3TUZudWVsM3NTcEg4RGhoWms0RUpJOFVEbzd5cm5CWU94RktZND0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODA1YmUwMjQtYTIwOC00MGZiLWFiNmYtMzk5YzI2NDdkMzM0IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJhdmFuaTEzNzA5QG1hc3Rlay5jb20iLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMjAzLjE5NC4xMDQuMTYiLCJuYW1lIjoiQXZhbmkgU2FsZWthciIsIm9pZCI6ImFmN2Q4M2NiLWRhZWQtNGNiZi05M2ViLTc0NWNhNmNjYjZhNCIsInJoIjoiMC5BUUlBU01vMGtUMW1CVXFXaWpHa0x3cnRQaVRnVzRBSW92dEFxMjg1bkNaSDB6UUNBSWcuIiwicm9sZXMiOlsiQmF0Y2hDcmVhdGUiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoicjdpcVRxMTNMTEM5RHhlN0JmVy1udHR4amcwRzk1ZjhXaENUa2E0Ukx0MCIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiYXZhbmkxMzcwOUBtYXN0ZWsuY29tIiwidXRpIjoiUjd3eG42VjRsVTY1aUFRTFZBWWhBQSIsInZlciI6IjEuMCJ9.MvZg0VYqlVhOimspmKuTQylqY7YrzFpCEXJSyZVt_7vB_Z6_Eu5DBMzr3DU-CH-zvTZ6nIEYR-H5mbvx7STNQyh2Uog_l2nZSpHSFyrnOHWiDQ6Y1jqArOIOdbcxP9tTp_V1EqagkLs51mY_TJbASiezVz9fU2_jaenHrV0FmCUaIDoc2xWLib54abv5LnlDisjqcfxc9t4AuuQ-b363xamS70VpHjkneY4DirDRQZRWnwZ2AoKNm0wYP-BG4OCPJBxUgB_qLw6UQeM7oc3ktJztGSONAmW0ndVdpyeS8HMltVq8KF-m1uVExV6-JWAcl4tZyWuTDGorl9GS7ZVKmw";
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

        public async Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products)
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
                httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri);

                if (httpResponse.IsSuccessStatusCode)
                {
                    uri = await SelectLatestPublishedDateBatch(products, internalSearchBatchResponse, uri, httpResponse, productList);
                }
                else
                {
                    logger.LogInformation(EventIds.QueryFileShareServiceNonOkResponse.ToEventId(), "File share service with uri {RequestUri} and responded with {StatusCode}", httpResponse.RequestMessage.RequestUri, httpResponse.StatusCode);
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
        public async Task<bool> DownloadReadMeTextFile(string batchId)
        {
            string payloadJson = string.Empty;
            bool result = false;
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var uri = $"{fileShareServiceConfig.Value.BaseUrl}/batch?$filter={fileShareServiceConfig.Value.ProductType} fileName eq {fileShareServiceConfig.Value.ReadMeFileName} and BusinessUnit eq '{fileShareServiceConfig.Value.BusinessUnit}'";
            HttpResponseMessage httpResponse;
            httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Get, payloadJson, accessToken, uri);
            if (httpResponse.IsSuccessStatusCode)
            {
                var filePath = string.Format(fileShareServiceConfig.Value.FileDownloadPath, DateTime.UtcNow.ToString("ddMMMyyyy"), batchId, "V01X01", "ENC_ROOT");
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                string path = Path.Combine(filePath, fileShareServiceConfig.Value.ReadMeFileName);
                using (Stream stream = await httpResponse.Content.ReadAsStreamAsync())
                {
                    using (FileStream outputFileStream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
                    {
                        outputFileStream.Position = 0;
                        int line_to_edit = 2;
                        StringBuilder lineToWrite = new StringBuilder();
                        lineToWrite.AppendLine("File date:");
                        lineToWrite.AppendLine(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string line = reader.ReadLine();
                            for (int i = 0; i <= line_to_edit; ++i)
                            {
                                if (line.Contains("Last"))
                                {
                                    line = lineToWrite.ToString();
                                }
                            }
                        }
                        stream.CopyTo(outputFileStream);
                        result = true;
                    }
                }
            }
            return result;
        }
    }
}
