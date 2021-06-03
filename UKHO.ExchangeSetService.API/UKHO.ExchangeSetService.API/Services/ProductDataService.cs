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
using UKHO.ExchangeSetService.Common.Storage;

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
        private readonly IExchangeSetStorageProvider exchangeSetStorageProvider;      


        public ProductDataService(IProductIdentifierValidator productIdentifierValidator,
            IProductDataProductVersionsValidator productVersionsValidator, 
            IProductDataSinceDateTimeValidator productDataSinceDateTimeValidator,
            ISalesCatalogueService salesCatalougeService,
            IMapper mapper,
            IFileShareService fileShareService,
            ILogger<FileShareService> logger,IExchangeSetStorageProvider exchangeSetStorageProvider
            )
        {
            this.productIdentifierValidator = productIdentifierValidator;
            this.productVersionsValidator = productVersionsValidator;
            this.productDataSinceDateTimeValidator = productDataSinceDateTimeValidator;
            this.salesCatalogueService = salesCatalougeService;
            this.mapper = mapper;
            this.fileShareService = fileShareService;
            this.logger = logger;
            this.exchangeSetStorageProvider = exchangeSetStorageProvider;          
        }

        public async Task<ExchangeSetServiceResponse> CreateProductDataByProductIdentifiers(ProductIdentifierRequest productIdentifierRequest)
        {
            var salesCatalogueResponse = await salesCatalogueService.PostProductIdentifiersAsync(productIdentifierRequest.ProductIdentifier.ToList());
            ////can check for file size from salesCatalogueResponse.ResponseCode
            var response = SetExchangeSetResponse(salesCatalogueResponse, false);
            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await GetSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productIdentifierRequest.CallbackUri, productIdentifierRequest.CorrelationId);
            }

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
            if (response.HttpStatusCode != HttpStatusCode.OK && response.HttpStatusCode != HttpStatusCode.NotModified)
            {
                return response;
            }
 
            if (salesCatalogueResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                response.ExchangeSetResponse.RequestedProductCount = response.ExchangeSetResponse.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
                salesCatalogueResponse.ResponseBody = new SalesCatalogueProductResponse
                {
                    Products = new List<Products>(),
                    ProductCounts = new ProductCounts()
                };
                salesCatalogueResponse.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned = new List<RequestedProductsNotReturned>();
                salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = request.ProductVersions.Count;
            }

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await GetSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, request.CallbackUri , request.CorrelationId);
            }

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
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return response;
            }            

            var exchangeSetServiceResponse = await SetExchangeSetResponseLinks(response);

            if (!string.IsNullOrEmpty(exchangeSetServiceResponse.BatchId))
            {
                await GetSalesCatalogueStorageDetails(salesCatalogueResponse.ResponseBody, exchangeSetServiceResponse.BatchId, productDataSinceDateTimeRequest.CallbackUri, productDataSinceDateTimeRequest.CorrelationId);
            }

            return response;
        }

        public Task<ValidationResult> ValidateProductDataSinceDateTime(ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest)
        {
            return productDataSinceDateTimeValidator.Validate(productDataSinceDateTimeRequest);
        }

        private ExchangeSetServiceResponse SetExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse, bool isNotModifiedToOk)
        {
            var response = new ExchangeSetServiceResponse
            {
                HttpStatusCode = salesCatalougeResponse.ResponseCode
            };
            if (salesCatalougeResponse.ResponseCode == HttpStatusCode.OK)
            {                
                response.ExchangeSetResponse = MapExchangeSetResponse(salesCatalougeResponse);       
                response.LastModified = ConvertLastModifiedToString(salesCatalougeResponse);
            }
            else if (salesCatalougeResponse.ResponseCode == HttpStatusCode.NotModified)
            {
                if (isNotModifiedToOk)
                {
                    response.HttpStatusCode = HttpStatusCode.OK;
                }
                response.ExchangeSetResponse = new ExchangeSetResponse();
                response.LastModified = ConvertLastModifiedToString(salesCatalougeResponse);
            }
            else
            {
                response.HttpStatusCode = HttpStatusCode.InternalServerError;
            }
            return response;
        }

        private async Task<ExchangeSetServiceResponse> SetExchangeSetResponseLinks(ExchangeSetServiceResponse exchangeSetResponse)
        {
            logger.LogInformation(EventIds.FSSCreateBatchRequestStart.ToEventId(), $"FSS create batch endpoint request started");

            var createBatchResponse = await fileShareService.CreateBatch();

            if (createBatchResponse.ResponseCode != HttpStatusCode.Created)
            {
                exchangeSetResponse = new ExchangeSetServiceResponse
                {
                    HttpStatusCode = HttpStatusCode.InternalServerError
                };
                return exchangeSetResponse;
            }

            exchangeSetResponse.ExchangeSetResponse.Links = new Links()
            {
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = createBatchResponse.ResponseBody.BatchStatusUri },
                ExchangeSetFileUri = new LinkSetFileUri { Href = createBatchResponse.ResponseBody.ExchangeSetFileUri }
            };
            exchangeSetResponse.ExchangeSetResponse.ExchangeSetUrlExpiryDateTime = Convert.ToDateTime(createBatchResponse.ResponseBody.BatchExpiryDateTime).ToUniversalTime();
            exchangeSetResponse.BatchId = createBatchResponse.ResponseBody.BatchId;

            logger.LogInformation(EventIds.FSSCreateBatchRequestCompleted.ToEventId(), "FSS create batch endpoint request completed with batch status uri {ExchangeSetBatchStatusUri.Href}", exchangeSetResponse.ExchangeSetResponse?.Links.ExchangeSetBatchStatusUri.Href);

            return exchangeSetResponse;
        }

        private ExchangeSetResponse MapExchangeSetResponse(SalesCatalogueResponse salesCatalougeResponse)
        {
            var model = mapper.Map<ExchangeSetResponse>(salesCatalougeResponse.ResponseBody?.ProductCounts);
            model.RequestedProductsNotInExchangeSet = mapper.Map<IEnumerable<RequestedProductsNotInExchangeSet>>(salesCatalougeResponse.ResponseBody?.ProductCounts?.RequestedProductsNotReturned);
            return model;
        }

        private string ConvertLastModifiedToString(SalesCatalogueResponse salesCatalougeResponse)
        {
            return (salesCatalougeResponse.LastModified.HasValue) ? salesCatalougeResponse.LastModified.Value.ToString(RFC1123Format) : null; 
        }

        private async Task<bool> GetSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId)
        {
            logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "SCS response store request started for the {batchId}", batchId);

            bool result = await exchangeSetStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, correlationId);

            logger.LogInformation(EventIds.SCSResponseStoreRequestCompleted.ToEventId(), "SCS response store request completed for the {batchId}", batchId);
            return result;
        }


    }
}
