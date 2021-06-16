using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareServiceClient
    {
        public Task<HttpResponseMessage> CallFileShareServiceApi(HttpMethod method, string requestBody, string authToken, string uri, [Optional] string correlationId);
    }
}
