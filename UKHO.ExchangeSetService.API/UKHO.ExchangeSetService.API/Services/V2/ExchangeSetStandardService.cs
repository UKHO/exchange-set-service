// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services.V2
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IUpdatesSinceValidator _updatesSinceValidator;
        private readonly IProductVersionsValidator _productVersionsValidator;
        private readonly IProductNameValidator _productNameValidator;
        private readonly ISalesCatalogueService _salesCatalogueService;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger,
            IUpdatesSinceValidator updatesSinceValidator,
            IProductVersionsValidator productVersionsValidator,
            IProductNameValidator productNameValidator,
            ISalesCatalogueService salesCatalogueService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productVersionsValidator = productVersionsValidator ?? throw new ArgumentNullException(nameof(productVersionsValidator));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
            _salesCatalogueService = salesCatalogueService ?? throw new ArgumentNullException(nameof(salesCatalogueService));
        }

        /// <summary>
        /// Processes the product names request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="productNames">Array of product names to be processed.</param>
        /// <param name="apiVersion">API version V2 to be used.</param>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set (s100).</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
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

            return SetExchangeSetStandardResponse(productNamesRequest, salesCatalogServiceResponse);
        }

        /// <summary>
        /// Processes the product versions request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="productVersionRequest">Enumerable of product version requests to be processed.</param>
        /// <param name="apiVersion">API version V2 to be used.</param>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set (s100).</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
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
                salesCatalogServiceResponse.Value.ResponseBody = new SalesCatalogueProductResponse
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

        /// <summary>
        /// Processes the updates since request and returns the exchange set standard service response.
        /// </summary>
        /// <param name="updatesSinceRequest">Request containing the sinceDateTime parameter.</param>
        /// <param name="apiVersion">API version V2 to be used.</param>
        /// <param name="exchangeSetStandard">The standard of the Exchange Set (s100).</param>
        /// <param name="productIdentifier">Optional product identifier for filtering the updates.</param>
        /// <param name="callbackUri">Optional callback URI for notification once the Exchange Set is ready.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <param name="cancellationToken">If true then notifies the underlying connection is aborted thus request operations should be cancelled.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
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

        /// <summary>
        /// Sanitizes the product names by trimming and removing empty or null values.
        /// </summary>
        /// <param name="productNames">Enumerable of product names to be sanitized.</param>
        /// <returns>Array of sanitized product names.</returns>
        private static string[] SanitizeProductNames(IEnumerable<string> productNames)
        {
            return productNames?.Where(name => !string.IsNullOrEmpty(name))
                                .Select(name => name.Trim())
                                .ToArray();
        }

        /// <summary>
        /// Validates the request and returns the validation result.
        /// </summary>
        /// <typeparam name="T">Type of the request to be validated.</typeparam>
        /// <param name="request">Request to be validated.</param>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <returns>Validation result containing the validation errors if any.</returns>
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

        /// <summary>
        /// Returns a bad request error response.
        /// </summary>
        /// <param name="correlationId">Guid based id for tracking the request.</param>
        /// <returns>Service response result containing the bad request error description.</returns>
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

        /// <summary>
        /// Sets the exchange set standard response based on the sales catalogue result.
        /// </summary>
        /// <typeparam name="R">Type of the request.</typeparam>
        /// <typeparam name="T">Type of the sales catalogue result.</typeparam>
        /// <param name="request">Request to be processed.</param>
        /// <param name="salesCatalogueResult">Sales catalogue result containing the response.</param>
        /// <returns>Service response result containing the exchange set standard service response.</returns>
        private static ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse<R, T>(R request, ServiceResponseResult<T> salesCatalogueResult)
        {
            var productCounts = (salesCatalogueResult.Value as SalesCatalogueResponse)?.ResponseBody?.ProductCounts;
            var lastModified = (salesCatalogueResult.Value as SalesCatalogueResponse)?.LastModified?.ToString("R");

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
    }
}
