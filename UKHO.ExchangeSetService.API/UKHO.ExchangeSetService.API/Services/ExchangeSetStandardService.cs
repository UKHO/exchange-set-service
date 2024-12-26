// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetStandardService : IExchangeSetStandardService
    {
        private readonly ILogger<ExchangeSetStandardService> _logger;
        private readonly IUpdatesSinceValidator _updatesSinceValidator;
        private readonly IProductNameValidator _productNameValidator;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger, IUpdatesSinceValidator updatesSinceValidator, IProductNameValidator productNameValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> CreateUpdatesSince(UpdatesSinceRequest updatesSinceRequest, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.CreateUpdatesSinceStarted.ToEventId(), "Create update since started | _X-Correlation-ID : {correlationId}", correlationId);

            if (updatesSinceRequest == null)
            {
                return BadRequestErrorResponse(correlationId);
            }

            updatesSinceRequest.ProductIdentifier = productIdentifier;
            updatesSinceRequest.CallbackUri = callbackUri;

            var validationResult = await ValidateUpdatesSinceRequest(updatesSinceRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            // This is a placeholder, the actual implementation is not provided
            var exchangeSetServiceResponse = new ExchangeSetStandardServiceResponse
            {
                BatchId = Guid.NewGuid().ToString(),
                LastModified = DateTime.UtcNow.ToString("R"),
                ExchangeSetStandardResponse = new ExchangeSetStandardResponse()
                {
                    BatchId = Guid.NewGuid().ToString()
                }
            };

            _logger.LogInformation(EventIds.CreateUpdatesSinceCompleted.ToEventId(), "Create update since completed | _X-Correlation-ID : {correlationId}", correlationId);
            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetServiceResponse);
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> CreateProductDataByProductNames(string[] productNames, string callbackUri, string correlationId)
        {
            _logger.LogInformation(EventIds.CreateProductDataByProductNamesStarted.ToEventId(), "Create product data for product Names started | _X-Correlation-ID : {correlationId}", correlationId);
            productNames = SanitizeProductNames(productNames);

            if (productNames == null || productNames.Length == 0)
            {
                _logger.LogError(EventIds.EmptyBodyError.ToEventId(), "Either body is null or malformed | _X-Correlation-ID : {correlationId}", correlationId);

                return BadRequestErrorResponse(correlationId);
            }

            var productNamesRequest = new ProductNameRequest()
            {
                ProductNames = productNames,
                CallbackUri = callbackUri,
                CorrelationId = correlationId
            };

            var validationResult = await ValidateProductNamesRequest(productNamesRequest, correlationId);
            if (validationResult != null)
            {
                return validationResult;
            }

            _logger.LogInformation(EventIds.CreateProductDataByProductNamesCompleted.ToEventId(), "Create Product data for product Names completed | _X-Correlation-ID : {correlationId}", correlationId);
            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(null); // This is a placeholder, the actual implementation is not provided
        }

        private string[] SanitizeProductNames(string[] productNames)
        {
            if (productNames == null)
            {
                return null;
            }

            if (productNames.Any(x => x == null))
            {
                return [null];
            }

            List<string> sanitizedIdentifiers = [];
            if (productNames.Length > 0)
            {
                foreach (var identifier in productNames)
                {
                    var sanitizedIdentifier = identifier.Trim();
                    sanitizedIdentifiers.Add(sanitizedIdentifier);
                }
            }

            return [.. sanitizedIdentifiers];
        }

        private async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ValidateUpdatesSinceRequest(UpdatesSinceRequest updatesSinceRequest, string correlationId)
        {
            var validationResult = await _updatesSinceValidator.Validate(updatesSinceRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.UpdatesSinceValidationFailed.ToEventId(), "Update since validation failed | _X-Correlation-ID : {correlationId}", correlationId);

                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
            }
            return null;
        }

        private async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ValidateProductNamesRequest(ProductNameRequest productNameRequest, string correlationId)
        {
            var validationResult = await _productNameValidator.Validate(productNameRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.InvalidProductNames.ToEventId(), "Product name validation failed. | _X-Correlation-ID : {correlationId}", correlationId);

                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
            }
            return null;
        }

        protected ServiceResponseResult<ExchangeSetStandardServiceResponse> BadRequestErrorResponse(string correlationId)
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
    }
}
