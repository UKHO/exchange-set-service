using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Request;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class FileShareService : IFileShareService
    {
        private readonly HttpClient httpClient;
        private readonly IAuthTokenProvider authTokenProvider;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;

        public FileShareService(HttpClient httpClient,
                                IAuthTokenProvider authTokenProvider,
                                IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
        {
            this.httpClient = httpClient;
            this.authTokenProvider = authTokenProvider;
            this.fileShareServiceConfig = fileShareServiceConfig;
        }
        public async Task<CreateBatchResponse> CreateBatch()
        {
            var accessToken = await authTokenProvider.GetManagedIdentityAuthAsync(fileShareServiceConfig.Value.ResourceId);
            var uri = $"/batch";

            CreateBatchRequest batch = new CreateBatchRequest
            {
                BusinessUnit = fileShareServiceConfig.Value.BusinessUnit,
                Attributes = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("Exchange Set Type", "Update"),
                    new KeyValuePair<string, string>("Media Type", "Zip"),
                    new KeyValuePair<string, string>("Produt Type", "AVCS")
                },
                ExpiryDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
             };

            string payloadJson = JsonConvert.SerializeObject(batch);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var httpResponse = await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);                
                var body = await httpResponse.Content.ReadAsStringAsync();
                
                return JsonConvert.DeserializeObject<CreateBatchResponse>(body);                           
            }                      
        }
    }
}
