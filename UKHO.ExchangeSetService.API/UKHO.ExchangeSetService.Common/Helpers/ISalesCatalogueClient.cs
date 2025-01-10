using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ISalesCatalogueClient
    {
        Task<HttpResponseMessage> CallSalesCatalogueServiceApi(HttpMethod method, string requestBody, string authToken, string uri, string correlationId = "", CancellationToken cancellationToken = default);
    }
}
