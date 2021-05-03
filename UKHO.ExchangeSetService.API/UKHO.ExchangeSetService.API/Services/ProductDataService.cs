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
        private readonly IProductDataValidator productDataValidator;

        public ProductDataService(IProductDataValidator productDataValidator)
        {
            this.productDataValidator = productDataValidator;
        }

        public async Task<ExchangeSetResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse
            {
                ExchangeSetCellCount = 15,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>
                {
                    new RequestedProductsNotInExchangeSet { ProductName = "GB123456", Reason = "productWithdrawn" },
                    new RequestedProductsNotInExchangeSet { ProductName = "GB123789", Reason = "invalidProduct" }
                },
                Links = new Links()
                {
                    ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272" },
                    ExchangeSetFileUri = new LinkSetFileUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip" }
                }
            };

            await Task.CompletedTask;
            return exchangeSetResponse;
        }

        public Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return productDataValidator.Validate(productDataSinceDateTimeRequest);
        }
    }
}
