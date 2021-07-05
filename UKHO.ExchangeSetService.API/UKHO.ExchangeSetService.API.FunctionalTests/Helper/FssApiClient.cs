using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class FssApiClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly string apiHost;

        public FssApiClient(string apiHost)
        {
            this.apiHost = apiHost;
        }

        /// <summary>
        /// Get Batch Status - GET /batch/{batchId}/status
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetBatchStatusAsync(string batchId, string accessToken = null)
        {
            string uri = $"{apiHost}/batch/{batchId}/status";

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        /// <summary>
        /// Search Using Query parameter  - GET /batch
        /// </summary>        
        /// <param name="filter">Search filter, pass null to get data without filter</param>
        /// <param name="limit">Page limit, pass null to get default value which is counfigurable and currently set to 10 </param>
        /// <param name="start">Page start value, pass null to get default value which is 0 </param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SearchBatchesAsync(string filter = null, int? limit = null, int? start = null, string accessToken = null)
        {
            string uri = $"{apiHost}/batch?";

            if (filter != null)
            {
                uri += $"$filter={filter}&";
            }
            if (limit.HasValue)
            {
                uri += $"limit={limit}&";
            }
            if (start.HasValue)
            {
                uri += $"start={start}";
            }

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
