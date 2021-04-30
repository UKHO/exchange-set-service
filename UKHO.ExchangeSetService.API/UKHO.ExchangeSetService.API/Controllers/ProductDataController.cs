using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
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
        /// <returns></returns>
        [HttpPost]
        [Route("/productdata/productVersions")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p> <p>If there are no updates for any of the productVersions, then the return will be a '200' response with an empty Exchange Set(containing just the latest PRODUCTS.TXT) and the exchangeSetCellCount will be 0.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request - there are one or more errors in the specified parameters")]
        public async Task<IActionResult> ProductVersions([FromBody] List<ProductVersionRequest> productVersionsRequest, string callbackUri)
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
            ProductVersionsRequest request = new ProductVersionsRequest();
            request.ProductVersions = productVersionsRequest;
            request.CallbackUri = callbackUri;

            var validationResult = await productDataService.ValidateCreateBatch(request);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return BuildBadRequestErrorResponse(errors);
                }
            }
            return Ok(await productDataService.GetProductVersions(request));
        }
    }
}
