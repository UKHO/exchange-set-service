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

        /// <summary>
        /// Provide all the releasable data after a datetime.
        /// </summary>
        /// <remarks>Given a datetime, build an Exchange Set of all the releasable ENC versions that have been issued since that datetime.</remarks>
        /// <param name="sinceDateTime">The date and time from which changes are requested. Any changes since the date will be returned. The value should be the Last-Modified date returned by the last request to this operation. The date format follows RFC 1123.
        /// <para>Example : Wed, 21 Oct 2015 07:28:00 GMT</para>
        /// </param>
        /// <param name="callbackUri">An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. If not specified, then no call back notification will be sent.</param>
        /// <response code="200">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="429">The user has sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        [HttpPost]
        [Route("/productData")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, description: "A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request - there are one or more errors in the specified parameters")]
        public virtual async Task<IActionResult> ProductDataSinceDateTime([FromQuery, SwaggerParameter(Required = true), SwaggerSchema(Format = "date-time")] string sinceDateTime,
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
