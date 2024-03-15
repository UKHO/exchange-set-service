using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
    [ServiceFilter(typeof(BespokeExchangeSetAuthorizationFilterAttribute))]
    public class ProductInformationController : BaseController<ProductInformationController>
    {
        public ProductInformationController(IHttpContextAccessor contextAccessor,
            ILogger<ProductInformationController> logger)
            : base(contextAccessor, logger)
        {
        }

        /// <summary>
        /// Provide ENC information from sales catalog service.
        /// </summary>
        /// <remarks>
        /// Given a list of ENC name identifiers, return all the versions of the ENCs from sales catalog service.
        /// </remarks>
        /// <param name="productIdentifiers">The JSON body containing product identifiers.</param>
        /// <response code="200">A JSON body that containing the information of ENCs.</response>
        /// <response code="400">Bad Request.</response>
        [HttpPost]
        [Route("/productInformation/productIdentifiers")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(SalesCatalogueResponse), description: "<p>A JSON body that containing the information of ENCs.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        public virtual IActionResult PostProductIdentifiers([FromBody] string[] productIdentifiers)
        {
            if (productIdentifiers == null || productIdentifiers.Length == 0)
            {
                var error = new List<Error>
                {
                    new()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }
            return StatusCode(StatusCodes.Status200OK);
        }
    }
}
