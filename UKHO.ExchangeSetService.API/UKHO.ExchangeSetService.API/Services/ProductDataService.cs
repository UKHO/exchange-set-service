using AutoMapper;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(productIdentifierRequest.ProductIdentifier.ToList());
            ////can check for file size from salesCatalogueResponse.ResponseCode
            var response = SetExchangeSetResponse(salesCatalogueResponse, false);
            if (response.HttpstatusCode != HttpStatusCode.OK && response.HttpstatusCode != HttpStatusCode.NotModified)
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

        public Task<ValidationResult> ValidateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            return productIdentifierValidator.Validate(productIdentifierRequest);
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            var salesCatalogueResponse = await salesCatalogueService.PostProductVersionsAsync(request.ProductVersions);
            ////can check for file size from salesCatalogueResponse
            var response = SetExchangeSetResponse(salesCatalogueResponse, true);
            if (response.HttpstatusCode != HttpStatusCode.OK && response.HttpstatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }
            //// FSS call for creating Batch and fill data for _links, exchangeSetUrlExpiryDateTime etc
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                response.ExchangeSetResponse.RequestedProductCount = response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count; 
            }
            response.ExchangeSetResponse.Links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272" },
                ExchangeSetFileUri = new LinkSetFileUri { Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip" }
            };

            return response;
        }

        public Task<ValidationResult> ValidateProductDataByProductVersions(ProductDataProductVersionsRequest request)
        {
            return productVersionsValidator.Validate(request);
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            var salesCatalogueResponse = await salesCatalogueService.GetProductsFromSpecificDateAsync(productDataSinceDateTimeRequest.SinceDateTime);
            ////can check for file size from salesCatalogueResponse
            var response = SetExchangeSetResponse(salesCatalogueResponse, false);
            if (response.HttpstatusCode != HttpStatusCode.OK)
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

        private ExchangeSetServiceResponse SetExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse, bool isNotModifiedToOk)
        {
            var response = new ExchangeSetServiceResponse();
            response.HttpstatusCode = salesCatalougeResponse.ResponseCode;
            if (salesCatalougeResponse.ResponseCode == HttpStatusCode.OK)
            {
                var model = mapper.Map<ExchangeSetResponse>(salesCatalougeResponse.ResponseBody?.ProductCounts);
                model.RequestedProductsNotInExchangeSet = mapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(salesCatalougeResponse.ResponseBody?.ProductCounts?.RequestedProductsNotReturned);
                response.ExchangeSetResponse = model;
            }
            else if (salesCatalougeResponse.ResponseCode == HttpStatusCode.NotModified && isNotModifiedToOk)
            {
                response.HttpstatusCode = HttpStatusCode.OK;
                response.ExchangeSetResponse = new ExchangeSetResponse();
            }
            else if (salesCatalougeResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                response.LastModified = salesCatalougeResponse.LastModified;
            }
            return response;
        }
    }
}
