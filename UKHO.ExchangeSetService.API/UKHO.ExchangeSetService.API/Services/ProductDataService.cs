using AutoMapper;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ProductDataService : IProductDataService
    {
        private readonly IProductIdentifierValidator productIdentifierValidator;
        private readonly IProductDataProductVersionsValidator productVersionsValidator;
        private readonly IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator;
        private readonly ISalesCatalogueService salesCatalogueService;
        private readonly IMapper mapper;

        public ProductDataService(IProductIdentifierValidator productIdentifierValidator,
            IProductDataProductVersionsValidator productVersionsValidator, 
            IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
            ISalesCatalogueService salesCatalougeService,
            IMapper mapper)
        {
            this.productIdentifierValidator = productIdentifierValidator;
            this.productVersionsValidator = productVersionsValidator;
            this.productDataSinceDateTimeValidator = productDataSinceDateTimeValidator;
            this.salesCatalogueService = salesCatalougeService;
            this.mapper = mapper;
        }

        public async Task<ExchangeSetResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Links links = new Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetFileUri = linkSetFileUri
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>()
            {
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
            ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                ExchangeSetCellCount = 15,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet
            };
            await Task.CompletedTask;
            return exchangeSetResponse;
        }

        public Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            return productIdentifierValidator.Validate(productIdentifierRequest);
        }

        public async Task<ExchangeSetResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request)
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

        public Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            return productVersionsValidator.Validate(request);
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            var response = SetExchangeSetResponse(await salesCatalogueService.GetProductsFromSpecificDateAsync(productDataSinceDateTimeRequest.SinceDateTime));
            if (response.HttpstatusCode != HttpStatusCode.OK || response.HttpstatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }
            //// FSS call for creating Batch and fill data for _links, exchangeSetUrlExpiryDateTime etc
            response.ExchangeSetResponse.Links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272" },
                ExchangeSetFileUri = new LinkSetFileUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip" }
            };

            return response;
        }

        public Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return productDataSinceDateTimeValidator.Validate(productDataSinceDateTimeRequest);
        }

        private ExchangeSetServiceResponse SetExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse)
        {
            var response = new ExchangeSetServiceResponse();
            response.HttpstatusCode = salesCatalougeResponse.ResponseCode;
            if (salesCatalougeResponse.ResponseCode == HttpStatusCode.OK || salesCatalougeResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                var model = mapper.Map<ExchangeSetResponse>(salesCatalougeResponse.ResponseBody?.ProductCounts);
                model.RequestedProductsNotInExchangeSet = mapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(salesCatalougeResponse.ResponseBody?.ProductCounts?.RequestedProductsNotReturned);
                response.ExchangeSetResponse = model;
            }
            return response;
        }
    }
}
