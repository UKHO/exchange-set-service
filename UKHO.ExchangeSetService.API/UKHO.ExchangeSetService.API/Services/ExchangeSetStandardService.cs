// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation.Results;
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
        private readonly IProductDataService _productDataService;
        private readonly IProductNameValidator _productNameValidator;

        public ExchangeSetStandardService(ILogger<ExchangeSetStandardService> logger, IUpdatesSinceValidator updatesSinceValidator, IProductDataService productDataService, IProductNameValidator productNameValidator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _updatesSinceValidator = updatesSinceValidator ?? throw new ArgumentNullException(nameof(updatesSinceValidator));
            _productDataService = productDataService ?? throw new ArgumentNullException(nameof(productDataService));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
        }

        public async Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> CreateUpdatesSince(UpdatesSinceRequest updatesSinceRequest, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken)
        {
            _logger.LogInformation(EventIds.CreateUpdatesSinceStarted.ToEventId(), "Creation of update since started | X-Correlation-ID : {correlationId}", correlationId);

            if (updatesSinceRequest == null)
            {
                _logger.LogError(EventIds.CreateUpdatesSinceException.ToEventId(), "Creation of update since exception occurred | X-Correlation-ID : {correlationId}", correlationId);

                return BadRequestErrorResponse(correlationId);
            }

            updatesSinceRequest.ProductIdentifier = productIdentifier;
            updatesSinceRequest.CallbackUri = callbackUri;

            var validationResult = await _updatesSinceValidator.Validate(updatesSinceRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out var errors))
            {
                _logger.LogError(EventIds.CreateUpdatesSinceException.ToEventId(), "Creation of update since exception occurred | X-Correlation-ID : {correlationId}", correlationId);

                return ServiceResponseResult<ExchangeSetStandardServiceResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
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

            _logger.LogInformation(EventIds.CreateUpdatesSinceCompleted.ToEventId(), "Creation of update since completed | X-Correlation-ID : {correlationId}", correlationId);

            return ServiceResponseResult<ExchangeSetStandardServiceResponse>.Accepted(exchangeSetServiceResponse);
        }

        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateProductDataByProductNames(string[] productNames, string callbackUri, string correlationId)
        {
            _logger.LogInformation(EventIds.CreateProductDataByProductNamesStarted.ToEventId(), "Creation of Product data for product Names started | X-Correlation-ID : {correlationId}", correlationId);
            productNames = SanitizeProductNames(productNames);

            if (productNames == null || productNames.Length == 0)
            {
                var error = new List<Error>
                        {
                            new()
                            {
                                Source = "requestBody",
                                Description = "Either body is null or malformed."
                            }
                        };
                _logger.LogError(EventIds.EmptyBodyError.ToEventId(), "Either body is null or malformed | X-Correlation-ID : {correlationId}", correlationId);
                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = error });
            }

            var productNamesRequest = new ProductNameRequest()
            {
                ProductNames = productNames,
                CallbackUri = callbackUri,
                CorrelationId = correlationId
            };

            var validationResult = await ValidateProductDataByProductNames(productNamesRequest);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    _logger.LogError(EventIds.InvalidProductNames.ToEventId(), "Product name validation failed. | X-Correlation-ID : {correlationId}", correlationId);
                    return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
                }
            }

            _logger.LogInformation(EventIds.CreateProductDataByProductNamesCompleted.ToEventId(), "Creation of Product data for product Names completed | X-Correlation-ID : {correlationId}", correlationId);
            return ServiceResponseResult<ExchangeSetResponse>.Accepted(null); // This is a placeholder, the actual implementation is not provided
        }

        private string[] SanitizeProductNames(string[] productNames)
        {
            if (productNames == null)
            {
                return null;
            }

            if (productNames.Any(x => x == null))
            {
                return new string[] { null };
            }

            List<string> sanitizedIdentifiers = new List<string>();
            if (productNames.Length > 0)
            {
                foreach (string identifier in productNames)
                {
                    string sanitizedIdentifier = identifier.Trim();
                    sanitizedIdentifiers.Add(sanitizedIdentifier);
                }
            }

            return sanitizedIdentifiers.ToArray();
        }

        private Task<ValidationResult> ValidateProductDataByProductNames(ProductNameRequest productNameRequest)
        {
            return _productNameValidator.Validate(productNameRequest);
        }

        protected ServiceResponseResult<ExchangeSetStandardServiceResponse> BadRequestErrorResponse(string correlationId)
        {
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
