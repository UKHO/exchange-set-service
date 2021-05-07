using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{


    public class ExchangeSetApiClient
    {
        static HttpClient httpClient = new HttpClient();
        private readonly string apiHost;
        public ExchangeSetApiClient(string apiHost)
        {
            this.apiHost = apiHost;
        }

        /// <summary>
        /// Provide all the releasable data after a datetime. - POST /productData
        /// </summary>
        /// <param name="sincedateTime">The date and time from which changes are requested which follows RFC1123 format</param>
		/// <param name="callbackUri">callbackUri, pass NULL to skip call back notification</param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetExchangeSetBasedOnDateTimeAsync(string sincedateTime = null, string callbackUri = null, string accessToken = null)
        {
            string uri = $"{apiHost}/productData";
            if (sincedateTime != null)
            {
                uri += $"?sinceDateTime={sincedateTime}&";
            }
            if (callbackUri != null)
            {
                uri += $"callbackuri={callbackUri}";
            }

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
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
