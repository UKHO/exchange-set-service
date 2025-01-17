// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using UKHO.ExchangeSetService.Common.Storage.V2;

namespace UKHO.ExchangeSetService.API.Services.V2
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IUpdatesSinceValidator _updatesSinceValidator;
        private readonly IProductVersionsValidator _productVersionsValidator;
        private readonly IProductNameValidator _productNameValidator;
        private readonly ISalesCatalogueService _salesCatalogueService;
        private readonly IExchangeSetServiceStorageProvider _exchangeSetServiceStorageProvider;
        private bool isEmptyExchangeSet = false;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger,
            IUpdatesSinceValidator updatesSinceValidator,
            IProductVersionsValidator productVersionsValidator,
            IProductNameValidator productNameValidator,
            ISalesCatalogueService salesCatalogueService,
            IExchangeSetServiceStorageProvider exchangeSetServiceStorageProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
            _salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
            _exchangeSetServiceStorageProvider = exchangeSetServiceStorageProvider ?? throw new ArgumentNullException(nameof(exchangeSetServiceStorageProvider));
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequestAsync(string[] productNames, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            productNames = SanitizeProductNames(productNames);

            if (productNames == null || productNames.Length == 0)
            {
                return BadRequestErrorResponse(correlationId);
            }

            var productNamesRequest = new ProductNameRequest
            {
                ProductNames = productNames,
                CallbackUri = callbackUri,
                CorrelationId = correlationId
            };

            var validationResult = await ValidateRequest(productNamesRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.PostProductNamesAsync(apiVersion, exchangeSetStandard, productNamesRequest.ProductNames, correlationId, cancellationToken);

            var essResponse = SetExchangeSetStandardResponse(productNamesRequest, salesCatalogServiceResponse);

            string expiryDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture); // Temp code to be removed once expiry date is available from FSS
            var tempbatchid = "Temp"+ Guid.NewGuid().ToString();

            CheckEmptyExchangeSet(essResponse.Value);

            var success = await SaveSalesCatalogueStorageDetails(salesCatalogServiceResponse.Value.ResponseBody, tempbatchid, callbackUri, exchangeSetStandard, correlationId, expiryDate, salesCatalogServiceResponse.Value.ScsRequestDateTime, isEmptyExchangeSet, essResponse.Value.ExchangeSetStandardResponse);
            if (!success)
            {
                _logger.LogInformation(EventIds.CreateProductDataError.ToEventId(), "ProcessProductNamesRequestAsync failed for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", tempbatchid, correlationId);
            }
            return essResponse;
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequestAsync(IEnumerable<ProductVersionRequest> productVersionRequest, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            if (productVersionRequest == null || !productVersionRequest.Any() || productVersionRequest.Any(pv => pv == null))
            {
                return BadRequestErrorResponse(correlationId);
            }

            var productVersionsRequest = new ProductVersionsRequest
            {
                ProductVersions = productVersionRequest,
                CallbackUri = callbackUri
            };

            var validationResult = await ValidateRequest(productVersionsRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.PostProductVersionsAsync(apiVersion, exchangeSetStandard, productVersionsRequest.ProductVersions, correlationId, cancellationToken);

            //to be used while calling SaveSalesCatalogueStorageDetails
            if (salesCatalogServiceResponse.Value?.ResponseCode == HttpStatusCode.NotModified)
            {
                salesCatalogServiceResponse.Value.ResponseBody = new V2SalesCatalogueProductResponse
                {
                    Products = [],
                    ProductCounts = new ProductCounts()
                };
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.ReturnedProductCount = 0;
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsNotReturned = [];
                salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductCount = salesCatalogServiceResponse.Value.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount = productVersionsRequest.ProductVersions.Count();
            }

            return SetExchangeSetStandardResponse(productVersionsRequest, salesCatalogServiceResponse);
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequestAsync(UpdatesSinceRequest updatesSinceRequest, ApiVersion apiVersion, string exchangeSetStandard, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            if (updatesSinceRequest?.SinceDateTime == null)
            {
                return BadRequestErrorResponse(correlationId);
            }

            updatesSinceRequest.ProductIdentifier = productIdentifier;
            updatesSinceRequest.CallbackUri = callbackUri;

            var validationResult = await ValidateRequest(updatesSinceRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            var salesCatalogServiceResponse = await _salesCatalogueService.GetProductsFromUpdatesSinceAsync(apiVersion, exchangeSetStandard, updatesSinceRequest, correlationId, cancellationToken);

            return SetExchangeSetStandardResponse(updatesSinceRequest, salesCatalogServiceResponse);
        }

        private static string[] SanitizeProductNames(IEnumerable<string> productNames)
        {
            return productNames?.Where(name => !string.IsNullOrEmpty(name))
                                .Select(name => name.Trim())
                                .ToArray();
        }

        private async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ValidateRequest<T>(T request, string correlationId)
        {
            var validationResult = request switch
            {
                UpdatesSinceRequest updatesSinceRequest => await _updatesSinceValidator.Validate(updatesSinceRequest),
                ProductNameRequest productNameRequest => await _productNameValidator.Validate(productNameRequest),
                ProductVersionsRequest productVersionsRequest => await _productVersionsValidator.Validate(productVersionsRequest),
                _ => throw new InvalidOperationException("Unsupported request type")
            };

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.ValidationFailed.ToEventId(), "Validation failed for {RequestType} | errors : {errors} | _X-Correlation-ID : {correlationId}", typeof(T).Name, JsonConvert.SerializeObject(errors), correlationId);
                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
            }
            return null;
        }

        private ServiceResponseResult<ExchangeSetStandardServiceResponse> BadRequestErrorResponse(string correlationId)
        {
            _logger.LogError(EventIds.EmptyBodyError.ToEventId(), "Either body is null or malformed | _X-Correlation-ID : {correlationId}", correlationId);

            var errorDescription = new ErrorDescription
            {
                CorrelationId = correlationId,
                Errors = new List<Error>
                {
                    new()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                }
            };
            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(errorDescription);
        }

        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(R request, ServiceResponseResult<T> salesCatalogueResult)
        {
            var productCounts = (salesCatalogueResult.Value as V2SalesCatalogueResponse)?.ResponseBody?.ProductCounts;
            var lastModified = (salesCatalogueResult.Value as V2SalesCatalogueResponse)?.LastModified?.ToString("R");

            return salesCatalogueResult.StatusCode switch
            {
                HttpStatusCode.OK => ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(new ExchangeSetStandardServiceResponse
                {
                    ExchangeSetStandardResponse = new ExchangeSetStandardResponse
                    {
                        RequestedProductCount = productCounts.RequestedProductCount ?? 0,
                        ExchangeSetProductCount = productCounts.ReturnedProductCount ?? 0,
                        RequestedProductsAlreadyUpToDateCount = productCounts.RequestedProductsAlreadyUpToDateCount ?? 0,
                        RequestedProductsNotInExchangeSet = productCounts.RequestedProductsNotReturned
                            .Select(x => new RequestedProductsNotInExchangeSet { ProductName = x.ProductName, Reason = x.Reason })
                            .ToList(),
                    },
                    LastModified = lastModified
                }),
                HttpStatusCode.NotModified when request is ProductVersionsRequest productVersionsRequest => ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(new ExchangeSetStandardServiceResponse
                {
                    ExchangeSetStandardResponse = new ExchangeSetStandardResponse()
                    {
                        RequestedProductCount = productVersionsRequest.ProductVersions.Count(),
                        RequestedProductsAlreadyUpToDateCount = productVersionsRequest.ProductVersions.Count(),
                        RequestedProductsNotInExchangeSet = [],
                        ExchangeSetProductCount = 0
                    },
                    LastModified = lastModified,
                }),
                HttpStatusCode.NotModified when request is UpdatesSinceRequest => ServiceResponseResult<ExchangeSetStandardServiceResponse>.NotModified(new ExchangeSetStandardServiceResponse
                {
                    ExchangeSetStandardResponse = new ExchangeSetStandardResponse(),
                    LastModified = lastModified,
                }),
                HttpStatusCode.BadRequest => ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(salesCatalogueResult.ErrorDescription),
                HttpStatusCode.NotFound => ServiceResponseResult<ExchangeSetStandardServiceResponse>.NotFound(salesCatalogueResult.ErrorResponse),
                _ => ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError()
            };
        }

        private Task<bool> SaveSalesCatalogueStorageDetails(V2SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, ExchangeSetStandardResponse exchangeSetStandardResponse)
        {
            return _logger.LogStartEndAndElapsedTimeAsync(EventIds.SCSResponseStoreRequestStart,
                EventIds.SCSResponseStoreRequestCompleted,
                "SCS response store request for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}",
                async () =>
                {
                    bool result = await _exchangeSetServiceStorageProvider.SaveSalesCatalogueStorageDetails(salesCatalogueResponse, batchId, callBackUri, exchangeSetStandard, correlationId, expiryDate, scsRequestDateTime, isEmptyEncExchangeSet, exchangeSetStandardResponse);

                    return result;
                }, batchId, correlationId);
        }

        private void CheckEmptyExchangeSet(ExchangeSetStandardServiceResponse exchangeSetServiceResponse)
        {
            isEmptyExchangeSet = exchangeSetServiceResponse.ExchangeSetStandardResponse.RequestedProductsNotInExchangeSet.Any();// when invalid enc or aio cell requested
        }
    }
}
