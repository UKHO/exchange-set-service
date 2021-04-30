using FluentValidation.Results;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IProductDataService
    {
        Task<ValidationResult> ValidateCreateBatch(ProductVersionsRequest request);
        Task<ExchangeSetResponse> GetProductVersions(ProductVersionsRequest request);
    }
}
