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
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services
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

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequest(string[] productNames, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
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

            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(null); // This is a placeholder, the actual implementation is not provided
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequest(IEnumerable<ProductVersionRequest> productVersionRequest, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken)
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

            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(null); // This is a placeholder, the actual implementation is not provided
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequest(UpdatesSinceRequest updatesSinceRequest, string exchangeSetStandard, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            if (updatesSinceRequest == null)
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

            var salesCatalogServiceResponse = await _salesCatalogueService.GetProductsFromSpecificDateAsync(exchangeSetStandard, updatesSinceRequest.SinceDateTime, correlationId);

            return SetExchangeSetStandardResponse(salesCatalogServiceResponse);
        }

        private string[] SanitizeProductNames(string[] productNames)
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
                Errors =
                [
                    new Error
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                ]
            };
            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(errorDescription);
        }

        private ServiceResponseResult<ExchangeSetStandardServiceResponse> SetExchangeSetStandardResponse(ServiceResponseResult<SalesCatalogueResponse> serviceResponseResult)
        {
            return serviceResponseResult.StatusCode switch
            {
                HttpStatusCode.OK or HttpStatusCode.NotModified => ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(MapExchangeSetResponse(serviceResponseResult.StatusCode, serviceResponseResult.Value)),
                _ => ServiceResponseResult<ExchangeSetStandardServiceResponse>.InternalServerError()
            };
        }

        private ExchangeSetStandardServiceResponse MapExchangeSetResponse(HttpStatusCode httpStatusCode, SalesCatalogueResponse salesCatalogueResponse)
        {
            return new ExchangeSetStandardServiceResponse
            {
                ExchangeSetStandardResponse = new ExchangeSetStandardResponse
                {
                    RequestedProductCount = (int)salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductCount,
                    ExchangeSetProductCount = httpStatusCode == HttpStatusCode.NotModified ? 0 : (int)salesCatalogueResponse.ResponseBody.ProductCounts.ReturnedProductCount,
                    RequestedProductsAlreadyUpToDateCount = (int)salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsAlreadyUpToDateCount,
                    RequestedProductsNotInExchangeSet = salesCatalogueResponse.ResponseBody.ProductCounts.RequestedProductsNotReturned.Select(x => new RequestedProductsNotInExchangeSet { ProductName = x.ProductName, Reason = x.Reason }).ToList(),
                },
                LastModified = salesCatalogueResponse.LastModified?.ToString("R"),
            };
        }
    }
}
