using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ProductDataService : IProductDataService
    {
        private readonly IProductDataProductVersionsValidator productProductVersionsValidator;

        public ProductDataService(IProductDataProductVersionsValidator productDataValidator)
        {
            this.productProductVersionsValidator = productDataValidator;
        }

        public async Task<ExchangeSetResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            var response = new ExchangeSetResponse();
            const int RequestedProductCount = 22;
            const int ExchangeSetCellCount = 15;
            const int RequestedProductsAlreadyUpToDateCount = 5;
            response.Links = new Links();
            response.Links.ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            response.Links.ExchangeSetFileUri = new LinkSetFileUri()
            {
                Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip"
            };
            response.ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime();
            response.RequestedProductCount = RequestedProductCount;
            response.ExchangeSetCellCount = ExchangeSetCellCount;
            response.RequestedProductsAlreadyUpToDateCount = RequestedProductsAlreadyUpToDateCount;
            response.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>() {
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                } 
            };
            await Task.CompletedTask;
            return response;
        }

        public Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            return productProductVersionsValidator.Validate(request);
        }
    }
}
