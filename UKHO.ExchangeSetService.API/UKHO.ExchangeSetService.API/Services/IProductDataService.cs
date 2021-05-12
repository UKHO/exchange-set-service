using FluentValidation.Results;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IProductDataService
    {
        Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest);

        Task<ExchangeSetResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest);

        Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request);

        Task<ExchangeSetResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request);

        Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);

        Task<ExchangeSetResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);
    }
}
