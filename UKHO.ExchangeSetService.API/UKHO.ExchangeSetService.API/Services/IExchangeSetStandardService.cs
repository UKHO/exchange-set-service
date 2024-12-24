using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IExchangeSetStandardService
    {
        Task<ServiceResponseResult<ExchangeSetResponse>> CreateProductDataByProductNames(string[] productNames, string callbackUri, string correlationId);

    }
}
