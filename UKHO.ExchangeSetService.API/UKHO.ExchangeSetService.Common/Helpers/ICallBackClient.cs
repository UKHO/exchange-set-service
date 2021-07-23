using System.Net.Http;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface ICallBackClient
    {
        Task CallBackApi(HttpMethod method, string requestBody, string uri, string correlationId = "");
    }
}
