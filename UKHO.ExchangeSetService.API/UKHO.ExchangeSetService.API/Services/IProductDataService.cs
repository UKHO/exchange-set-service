using FluentValidation.Results;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IProductDataService
    {
        Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest);

        Task<ValidationResult> ValidateScsProductDataByProductIdentifiers(ScsProductIdentifierRequest scsproductIdentifierRequest);

        Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest, AzureAdB2C azureAdB2C);

        Task<SalesCatalogueResponse> CreateProductDataByProductIdentifiers(ScsProductIdentifierRequest scsProductIdentifierRequest);

        Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request);

        Task<ExchangeSetServiceResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request, AzureAdB2C azureAdB2C);

        Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);

        Task<ExchangeSetServiceResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest, AzureAdB2C azureAdB2C);

        Task<SalesCatalogueResponse> GetProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);

        Task<ValidationResult> ValidateScsDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);

    }
}
