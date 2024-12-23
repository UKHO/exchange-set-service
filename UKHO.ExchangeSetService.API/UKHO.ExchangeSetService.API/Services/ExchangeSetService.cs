using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetService : IExchangeSetService
    {
        private readonly IProductDataService _productDataService;
        private readonly IProductNameValidator _productNameValidator;
        private readonly ILogger<ExchangeSetService> _logger;


        public ExchangeSetService(IProductDataService productDataService, IProductNameValidator productNameValidator, ILogger<ExchangeSetService> logger)
        {
            _productDataService = productDataService ?? throw new ArgumentNullException(nameof(productDataService));
            _productNameValidator = productNameValidator ?? throw new ArgumentNullException(nameof(productNameValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateProductDataByProductNames(string[] productNames, string callbackUri, string correlationId)
        {
            _logger.LogInformation(EventIds.CreateProductDataByProductNamesStarted.ToEventId(), "Creation of Product data started | X-Correlation-ID : {correlationId}", correlationId);
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

            _logger.LogInformation(EventIds.CreateProductDataByProductNamesCompleted.ToEventId(), "Creation of Product data completed | X-Correlation-ID : {correlationId}", correlationId);
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
    }
}
