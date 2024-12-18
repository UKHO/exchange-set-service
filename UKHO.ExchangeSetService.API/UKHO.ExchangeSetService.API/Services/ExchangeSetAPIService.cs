using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public class ExchangeSetAPIService : IExchangeSetService
    {
        private readonly IProductDataService _productDataService;

        public ExchangeSetAPIService(IProductDataService productDataService)
        {
            _productDataService = productDataService ?? throw new ArgumentNullException(nameof(productDataService));
        }

        public async Task<ServiceResponseResult<ExchangeSetResponse>> CreateProductDataByProductNames(string[] productNames, string callbackUri, string correlationId)
        {
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
                return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = error });
            }

            var productNamesRequest = new ProductIdentifierRequest()
            {
                ProductIdentifier = productNames,
                CallbackUri = callbackUri,
                CorrelationId = correlationId
            };

            var validationResult = await _productDataService.ValidateProductDataByProductIdentifiers(productNamesRequest);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return ServiceResponseResult<ExchangeSetResponse>.BadRequest(new ErrorDescription { CorrelationId = correlationId, Errors = errors });
                }
            }

            return ServiceResponseResult<ExchangeSetResponse>.Success(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
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
    }
}
