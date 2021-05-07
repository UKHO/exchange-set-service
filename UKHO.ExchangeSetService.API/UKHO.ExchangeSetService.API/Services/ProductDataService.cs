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
            await Task.CompletedTask;
            const int RequestedProductCount = 22;
            const int ExchangeSetCellCount = 15;
            const int RequestedProductsAlreadyUpToDateCount = 5;
            return new ExchangeSetResponse()
            {
                Links = new Links()
                {
                    ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri()
                    {
                        Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
                    },
                    ExchangeSetFileUri = new LinkSetFileUri()
                    {
                        Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip"
                    }
                },
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = RequestedProductCount,
                ExchangeSetCellCount = ExchangeSetCellCount,
                RequestedProductsAlreadyUpToDateCount = RequestedProductsAlreadyUpToDateCount,
                RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>() {
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
                }
            };
        }

        public Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            return productProductVersionsValidator.Validate(request);
        }
    }
}
