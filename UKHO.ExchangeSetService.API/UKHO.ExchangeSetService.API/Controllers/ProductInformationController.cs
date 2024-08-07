﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
    public class ProductInformationController : BaseController<ProductInformationController>
    {
        private readonly IProductDataService productDataService;
        public ProductInformationController(IHttpContextAccessor contextAccessor,
            ILogger<ProductInformationController> logger,
            IProductDataService productDataService)
            : base(contextAccessor, logger)
        {
            this.productDataService = productDataService;
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
        /// <response code="401">Unauthorised - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/productInformation/productIdentifiers")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(SalesCatalogueResponse), description: "<p>A JSON body that containing the information of ENCs.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time you should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual Task<IActionResult> PostProductIdentifiers([FromBody] string[] productIdentifiers)
        {
            return Logger.LogStartEndAndElapsedTimeAsync(EventIds.PostValidateProductIdentifiersRequestForScsResponseStart, EventIds.PostValidateProductIdentifiersRequestForScsResponseCompleted,
                "Validate Product Identifiers Endpoint request for _X-Correlation-ID:{correlationId}",
                async () =>
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
                    ScsProductIdentifierRequest scsProductIdentifierRequest = new ScsProductIdentifierRequest()
                    {
                        ProductIdentifier = productIdentifiers,
                        CorrelationId = GetCurrentCorrelationId()
                    };

                    var validationResult = await productDataService.ValidateScsProductDataByProductIdentifiers(scsProductIdentifierRequest);

                    if (!validationResult.IsValid)
                    {
                        List<Error> errors;

                        if (validationResult.HasBadRequestErrors(out errors))
                        {
                            return BuildBadRequestErrorResponse(errors);
                        }
                    }
                    var productDetail = await productDataService.CreateProductDataByProductIdentifiers(scsProductIdentifierRequest);
                    return GetScsResponse(productDetail);
                }, GetCurrentCorrelationId());
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
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(SalesCatalogueResponse), description: "<p>A JSON body that containing the information of ENC versions that have been issued since that datetime.</p>")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.NotModified, name: "Date", type: "string", description: "Returns the current date and time on the server and should be used in subsequent requests to the productData operation to ensure that there are no gaps due to minor time difference between your own and UKHO systems. The date format is in RFC 1123 format.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time you should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual Task<IActionResult> GetProductInformationSinceDateTime([FromQuery, SwaggerParameter(Required = true), SwaggerSchema(Format = "date-time")] string sinceDateTime)
        {
            return Logger.LogStartEndAndElapsedTimeAsync(EventIds.SCSGetProductDataSinceDateTimeRequestStart, EventIds.SCSGetProductDataSinceDateTimeRequestCompleted,
                "SCS Product Data SinceDateTime Endpoint request for _X-Correlation-ID:{correlationId} ",
                async () =>
                {
                    ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest = new ProductDataSinceDateTimeRequest()
                    {
                        SinceDateTime = sinceDateTime,
                        CorrelationId = GetCurrentCorrelationId()
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

                    var validationResult = await productDataService.ValidateScsDataSinceDateTime(productDataSinceDateTimeRequest);

                    if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out List<Error> errors))
                    {
                        return BuildBadRequestErrorResponse(errors);
                    }
                    var productDetail = await productDataService.GetProductDataSinceDateTime(productDataSinceDateTimeRequest);

                    return GetScsResponse(productDetail);
                }, GetCurrentCorrelationId());
        }
    }
}

