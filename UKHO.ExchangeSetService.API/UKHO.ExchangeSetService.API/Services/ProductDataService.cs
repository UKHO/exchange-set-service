using AutoMapper;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ProductDataService : IProductDataService
    {
        private const string RFC1123Format = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
        private readonly IProductIdentifierValidator productIdentifierValidator;
        private readonly IProductDataProductVersionsValidator productVersionsValidator;
        private readonly IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator;
        private readonly ISalesCatalogueService salesCatalogueService;
        private readonly IMapper mapper;
        private readonly IFileShareService fileShareService;
        private readonly ILogger<FileShareService> logger;

        public ProductDataService(IProductIdentifierValidator productIdentifierValidator,
            IProductDataProductVersionsValidator productVersionsValidator, 
            IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
            ISalesCatalogueService salesCatalougeService,
            IMapper mapper,
            IFileShareService fileShareService,
            ILogger<FileShareService> logger)
        {
            this.productIdentifierValidator = productIdentifierValidator;
            this.productVersionsValidator = productVersionsValidator;
            this.productDataSinceDateTimeValidator = productDataSinceDateTimeValidator;
            this.salesCatalogueService = salesCatalougeService;
            this.mapper = mapper;
            this.fileShareService = fileShareService;
            this.logger = logger;
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
            logger.LogInformation(EventIds.FSSCreateProductDataByProductIdentifiersCreateBatchRequestStart.ToEventId(), $"FSS create batch for create product data by product identifiers endpoint started");

            response = await SetExchangeSetResponseLinks(response);

            logger.LogInformation(EventIds.FSSCreateProductDataByProductIdentifiersCreateBatchRequestCompleted.ToEventId(), "FSS create batch {ExchangeSetBatchStatusUri} for create product data by product identifiers endpoint completed", response.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);

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

            logger.LogInformation(EventIds.FSSCreateProductDataByProductVersionsCreateBatchRequestStart.ToEventId(), $"FSS create batch for create product data by product versions endpoint started");

            response = await SetExchangeSetResponseLinks(response);

            logger.LogInformation(EventIds.FSSCreateProductDataByProductVersionsCreateBatchRequestCompleted.ToEventId(), "FSS create batch {ExchangeSetBatchStatusUri} for create product data by product versions endpoint completed", response.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);

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
            logger.LogInformation(EventIds.FSSCreateProductDataSinceDateTimeCreateBatchRequestStart.ToEventId(), $"FSS create batch for create product data since date time endpoint started");

            response =await SetExchangeSetResponseLinks(response);

            logger.LogInformation(EventIds.FSSCreateProductDataSinceDateTimeCreateBatchRequestCompleted.ToEventId(), "FSS created batch {ExchangeSetBatchStatusUri} for create product data since date time endpoint completed", response.ExchangeSetResponse.Links.ExchangeSetBatchStatusUri);

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
                response.LastModified = salesCatalougeResponse.LastModified.ToString(RFC1123Format);
            }
            return response;
        }

        private async Task<ExchangeSetServiceResponse> SetExchangeSetResponseLinks(ExchangeSetServiceResponse exchangeSetResponse)
        {
            var createBatchResponse = await fileShareService.CreateBatch();

            if (createBatchResponse.ResponseCode != HttpStatusCode.Created)
            {
                exchangeSetResponse.ExchangeSetResponse.Links = new Links()
                {
                    ExchangeSetBatchStatusUri = null,
                    ExchangeSetFileUri = null
                };
                return exchangeSetResponse;
            }

            exchangeSetResponse.ExchangeSetResponse.Links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = createBatchResponse.ResponseBody.BatchStatusUri },
                ExchangeSetFileUri = new LinkSetFileUri { Href = createBatchResponse.ResponseBody.ExchangeSetFileUri }
            };
            exchangeSetResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(createBatchResponse.ResponseBody.BatchExpiryDateTime).ToUniversalTime();
            
            return exchangeSetResponse;
        }
    }
}
