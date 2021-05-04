using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    public class ProductDataController : BaseController<ProductDataController>
    {
        private readonly IProductDataService productDataService;

        public ProductDataController(IHttpContextAccessor contextAccessor,
           ILogger<ProductDataController> logger, IProductDataService productDataService
          )
       : base(contextAccessor, logger)
        {
            this.productDataService = productDataService;
        }

        /// <summary>
        /// Given a set of ENC versions (e.g. Edition x Update y) provide any later releasable files.
        /// </summary>
        /// <remarks>
        /// Given a list of ENC name identifiers and their edition and update numbers, return all the versions of the ENCs that are releasable from that version onwards.
        /// ## Business Rules:
        /// If there is no update to the version that is requested, then nothing will be returned for the ENC.
        /// 
        /// If none of the ENCs requested have an update, then a 'Not modified' response will be returned. If none of the ENCs requested exist, then a 'Bad Request' response will be returned.
        /// 
        /// The rules around cancellation, replacements, additional coverage and re-issues apply as defined in the previous section.
        /// </remarks>
        /// <param name="productVersionsRequest">The JSON body containing product versions.</param>
        /// <param name="callbackUri">An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. If not specified, then no call back notification will be sent.</param>
        /// <response code="200">The user has sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="429">The user has sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/productData/productVersions")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p> <p>If there are no updates for any of the productVersions, then the return will be a '200' response with an empty Exchange Set(containing just the latest PRODUCTS.TXT) and the exchangeSetCellCount will be 0.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time the user should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual async Task<IActionResult> PostProductDataByProductVersions([FromBody] List<ProductVersionRequest> productVersionsRequest, string callbackUri)
        {
            if (productVersionsRequest == null || !productVersionsRequest.Any())
            {
                var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "RequestBody",
                        Description = "Either body is null or malformed."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }
            ProductDataProductVersionsRequest request = new ProductDataProductVersionsRequest();
            request.ProductVersions = productVersionsRequest;
            request.CallbackUri = callbackUri;

            var validationResult = await productDataService.ValidateProductDataByProductVersions(request);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return BuildBadRequestErrorResponse(errors);
                }
            }
            return Ok(await productDataService.CreateProductDataByProductVersions(request));
        }
    }
}
