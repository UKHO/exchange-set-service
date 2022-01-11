using FluentValidation.Results;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IProductDataService
    {
        Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest);

        Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest, AzureAdB2C azureAdB2C);

        Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request);

        Task<ExchangeSetServiceResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request, AzureAdB2C azureAdB2C);

        Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest);

        Task<ExchangeSetServiceResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest, AzureAdB2C azureAdB2C);

        Task<ValidationResult> ValidateEventGridCacheDataRequest(EventGridCacheDataRequest eventGridCacheDataRequest);
        Task<bool> DeleteSearchAndDownloadCacheData(EventGridCacheDataRequest eventGridCacheDataRequest);
    }
}
