using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ProductInformationController : BaseController<ProductInformationController>
    {
        public ProductInformationController(IHttpContextAccessor contextAccessor,
            ILogger<ProductInformationController> logger)
            : base(contextAccessor, logger)
        {
        }

        /// <summary>
        /// Provide all the releasable data after a datetime.
        /// </summary>
        /// <remarks>Given a datetime, get all the releasable ENC versions that have been issued since that datetime.</remarks>
        /// <param name="sinceDateTime" example="Wed, 21 Oct 2015 07:28:00 GMT" >The date and time from which changes are requested. Any changes since the date will be returned. The value should be the value in the `Date` header returned by the last request to this operation. The date is in RFC 1123 format. The date and time must be within 28 days and cannot be in future.
        /// <br/><para><i>Example</i> : Wed, 21 Oct 2015 07:28:00 GMT</para>
        /// </param>
        /// <response code="200">A JSON body that containing the information of ENC versions that have been issued since that datetime.</response>
        /// <response code="304">Not modified.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorised - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpGet]
        [Route("/productInformation")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.OK, name: "Date", type: "string", description: "Returns the current date and time on the server and should be used in subsequent requests to the productData operation to ensure that there are no gaps due to minor time difference between your own and UKHO systems. The date format is in RFC 1123 format.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p><p>If there are no updates since the sinceDateTime parameter, then a 'Not modified' response will be returned.</p>")]
        public virtual IActionResult GetProductInformationSinceDateTime([FromQuery, SwaggerParameter(Required = true), SwaggerSchema(Format = "date-time")] string sinceDateTime)
        {
            ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest = new ProductDataSinceDateTimeRequest()
            {
                SinceDateTime = sinceDateTime
            };

            if (productDataSinceDateTimeRequest.SinceDateTime == null)
            {
                var error = new List<Error>
                {
                    new()
                    {
                        Source = "sinceDateTime",
                        Description = "Query parameter 'sinceDateTime' is required."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }
            return StatusCode(StatusCodes.Status200OK);
        }
    }
}
