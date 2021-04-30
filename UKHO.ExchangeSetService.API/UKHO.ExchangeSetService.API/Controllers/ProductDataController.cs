using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    public class ProductDataController : BaseController<ProductDataController>
    {
        private readonly IProductDataService productDataService;

        public ProductDataController(IHttpContextAccessor contextAccessor,
           ILogger<ProductDataController> logger,
           IProductDataService productDataService)
        : base(contextAccessor, logger)
        {
            this.productDataService = productDataService;
        }

        [HttpPost]
        [Route("/productData")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, description: "OK")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request - there are one or more errors in the specified parameters")]
        public virtual async Task<IActionResult> ProductDataSinceDateTime([FromQuery, SwaggerParameter(Required = true)] string sinceDateTime,
            [FromQuery] string callbackUri)
        {
            ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest = new ProductDataSinceDateTimeRequest()
            {
                SinceDateTime = sinceDateTime,
                CallbackUri = callbackUri
            };

            if (productDataSinceDateTimeRequest.SinceDateTime == null)
            {
                var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "QueryParameter",
                        Description = "QueryParameter is null or malformed."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }

            var validationResult = await productDataService.ValidateProductDataSinceDateTime(productDataSinceDateTimeRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out List<Error> errors))
            {
                return BuildBadRequestErrorResponse(errors);
            }

            var productDetail = await productDataService.CreateProductDataSinceDateTime(productDataSinceDateTimeRequest);
            return Ok(productDetail);
        }
    }
}
