using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models;
using System.Collections.Generic;
using System.Linq;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.API.Extensions;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetControllerBase<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;
        private readonly IExchangeSetService _exchangeSetService;
        private readonly IProductDataService _productDataService;

        public ExchangeSetController(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ExchangeSetController> logger,
            IExchangeSetService exchangeSetService,
            IProductDataService productDataService
            ) : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeSetService = exchangeSetService ?? throw new ArgumentNullException(nameof(exchangeSetService));
            _productDataService = productDataService ?? throw new ArgumentNullException(nameof(productDataService));
        }

        [HttpPost("{exchangeSetStandard}/productNames")]
        public IActionResult ProductNames([FromBody] string[] productNames, [FromQuery] string callbackUri)
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
                return BadRequest(error);
            }

            var productNamesRequest = new ProductIdentifierRequest()
            {
                ProductIdentifier = productNames,
                CallbackUri = callbackUri,
                CorrelationId = GetCorrelationId()
            };

            var validationResult = _productDataService.ValidateProductDataByProductIdentifiers(productNamesRequest).Result;

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return BadRequest(errors);
                }
            }
            var result = ExchangeSetServiceResponseResult<ExchangeSetResponse>.Success(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
            return result.StatusCode switch
            {
                HttpStatusCode.Accepted => Accepted(result.Value),
                HttpStatusCode.BadRequest => BadRequest(result.ErrorResponse),
                _ => StatusCode((int)result.StatusCode)
            };
        }

        private string[] SanitizeProductNames(string[] productIdentifiers)
        {
            if (productIdentifiers == null)
            {
                return null;
            }

            if (productIdentifiers.Any(x => x == null))
            {
                return new string[] { null };
            }

            List<string> sanitizedIdentifiers = new List<string>();
            if (productIdentifiers.Length > 0)
            {
                foreach (string identifier in productIdentifiers)
                {
                    string sanitizedIdentifier = identifier.Trim();
                    sanitizedIdentifiers.Add(sanitizedIdentifier);
                }
            }

            return sanitizedIdentifiers.ToArray();
        }
    }
}
