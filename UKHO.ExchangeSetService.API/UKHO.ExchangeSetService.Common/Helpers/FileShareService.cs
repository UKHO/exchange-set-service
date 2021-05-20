using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService : IFileShareService
    {
        private readonly IFileShareServiceClient fileShareServiceClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly ILogger<FileShareService> logger;
        private string batchExpiryDateTime;

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
            batchExpiryDateTime =createBatchRequest.ExpiryDate;

            string payloadJson = JsonConvert.SerializeObject(createBatchRequest);

            var httpResponse = await fileShareServiceClient.CallFileShareServiceApi(HttpMethod.Post, payloadJson, accessToken, uri);

            CreateBatchResponse createBatchResponse = await CreateBatchResponse(httpResponse);
            return createBatchResponse;                               
        }

        private async Task<CreateBatchResponse> CreateBatchResponse(HttpResponseMessage httpResponse)
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
    }
}
