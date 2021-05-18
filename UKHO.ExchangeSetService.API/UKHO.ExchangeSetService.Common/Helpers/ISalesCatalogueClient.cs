using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ISalesCatalogueClient
    {
        public Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string postBody, string authToken, string uri);
    }
}
