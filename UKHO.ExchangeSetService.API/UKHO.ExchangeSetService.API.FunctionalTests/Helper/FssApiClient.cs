using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class FssApiClient
    {
        static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Get Batch Status
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken = null)
        {
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
        /// Get File Download
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="fileRangeHeader">File Range Header, pass NULL to skip partial download</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetFileDownloadAsync(string uri, string fileRangeHeader = null, string accessToken = null)
        {

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (fileRangeHeader != null)
                {
                    httpRequestMessage.Headers.Add("Range", fileRangeHeader);
                }

                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        /// <summary>
        /// Search Using Query parameter
        /// </summary>  
        /// <param name="baseUri">Search filter, pass null to get data without filter</param>
        /// <param name="filter">Search filter, pass null to get data without filter</param>
        /// <param name="limit">Page limit, pass null to get default value which is counfigurable and currently set to 10 </param>
        /// <param name="start">Page start value, pass null to get default value which is 0 </param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SearchBatchesAsync(string baseUri, string filter = null, int? limit = null, int? start = null, string accessToken = null)
        {
            string uri = $"{baseUri}/batch?";

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
