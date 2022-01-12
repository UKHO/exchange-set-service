using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Extensions;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ApiController]
    [Authorize]
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
        /// Provide all the latest releasable baseline data for a specified set of ENCs.
        /// </summary>
        /// <remarks>
        /// Given a list of ENC name identifiers, return all the versions of the ENCs that are releasable and that are needed to bring the ENCs up to date, namely the base edition and any updates or re-issues applied to it.
        /// ## Business Rules:
        /// Only ENCs that are releasable at the date of the request will be returned.
        /// 
        /// For cancellation updates, all the updates up to the cancellation need to be included. Cancellations will be included for 12 months after the cancellation, as per the S63 specification.
        /// 
        /// If an ENC has a re-issue, then the latest batch on the FSS will be used. 
        /// 
        /// If a requested ENC has been cancelled and replaced or additional coverage provided, then the replacement or additional coverage ENC will not be included in the response payload. Only the specific ENCs requested will be returned. The current UKHO services (Planning Station/Gateway) are the same, they only give the user the data they ask for (i.e. if they ask for a cell that is cancelled, they only get the data for the cell that was cancelled).
        /// 
        /// If a requested ENC does not exist (it is not a valid ENC) then nothing for that ENC will be returned (i.e. the user is not informed it does not exist). If none of the requested ENCs exist, then status code 400 ('Bad Request') response will be returned.
        /// </remarks>
        /// <param name="productIdentifiers">The JSON body containing product identifiers.</param>
        /// <param name="callbackUri">An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. If not specified, then no call back notification will be sent.</param>
        /// <response code="200">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorised - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/productData/productIdentifiers")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time you should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual async Task<IActionResult> PostProductIdentifiers([FromBody] string[] productIdentifiers, [FromQuery] string callbackUri)
        {
            Logger.LogInformation(EventIds.ESSPostProductIdentifiersRequestStart.ToEventId(), "Product Identifiers Endpoint Started  for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());

            if (productIdentifiers == null || productIdentifiers.Length == 0)
            {
                var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }
            ProductIdentifierRequest productIdentifierRequest = new ProductIdentifierRequest()
            {
                ProductIdentifier = productIdentifiers,
                CallbackUri = callbackUri,
                CorrelationId = GetCurrentCorrelationId()
            };

            var validationResult = await productDataService.ValidateProductDataByProductIdentifiers(productIdentifierRequest);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return BuildBadRequestErrorResponse(errors);
                }
            }
            AzureAdB2C azureAdB2C = new AzureAdB2C()
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };

            var productDetail = await productDataService.CreateProductDataByProductIdentifiers(productIdentifierRequest, azureAdB2C);

            if (productDetail.IsExchangeSetTooLarge)
            {
                Logger.LogError(EventIds.ExchangeSetTooLarge.ToEventId(), "Requested exchange set is too large for product identifiers endpoint for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());
                return BuildBadRequestErrorResponseForTooLargeExchangeSet();
            }
            Logger.LogInformation(EventIds.ESSPostProductIdentifiersRequestCompleted.ToEventId(), "Product Identifiers Endpoint Completed for BatchId:{batchId} and _X-Correlation-ID:{correlationId}", productDetail.BatchId, GetCurrentCorrelationId());

            return GetEssResponse(productDetail);
        }

        /// <summary>
        /// Given a set of ENC versions (e.g. Edition x Update y) provide any later releasable files.
        /// </summary>
        /// <remarks>
        /// Given a list of ENC name identifiers and their edition and update numbers, return all the versions of the ENCs that are releasable from that version onwards.
        /// ## Business Rules:
        /// If there is no update to the version that is requested, then nothing will be returned for the ENC.
        /// 
        /// If none of the ENCs requested have an update, then a 'Not modified' response will be returned. If none of the ENCs requested exist, then status code 400 ('Bad Request') response will be returned.
        /// 
        /// The rules around cancellation, replacements, additional coverage and re-issues apply as defined in the previous section.
        /// </remarks>
        /// <param name="productVersionsRequest">The JSON body containing product versions.</param>
        /// <param name="callbackUri">An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. If not specified, then no call back notification will be sent.</param>
        /// <response code="200">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set. If there are no updates for any of the productVersions, then status code 200 ('OK') will be returned with an empty Exchange Set (containing just the latest PRODUCTS.TXT) and the exchangeSetCellCount will be 0.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorised - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/productData/productVersions")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p><p>If there are no updates for any of the productVersions, then status code 200 ('OK') will be returned with an empty Exchange Set (containing just the latest PRODUCTS.TXT) and the exchangeSetCellCount will be 0.</p>")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time you should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual async Task<IActionResult> PostProductDataByProductVersions([FromBody] List<ProductVersionRequest> productVersionsRequest, string callbackUri)
        {
            Logger.LogInformation(EventIds.ESSPostProductVersionsRequestStart.ToEventId(), "Product Versions Endpoint Started for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());

            if (productVersionsRequest == null || !productVersionsRequest.Any())
            {
                var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "requestBody",
                        Description = "Either body is null or malformed."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }
            ProductDataProductVersionsRequest request = new ProductDataProductVersionsRequest
            {
                ProductVersions = productVersionsRequest,
                CallbackUri = callbackUri,
                CorrelationId = GetCurrentCorrelationId()
            };

            var validationResult = await productDataService.ValidateProductDataByProductVersions(request);

            if (!validationResult.IsValid)
            {
                List<Error> errors;

                if (validationResult.HasBadRequestErrors(out errors))
                {
                    return BuildBadRequestErrorResponse(errors);
                }
            }
            AzureAdB2C azureAdB2C = new AzureAdB2C()
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };

            var productDetail = await productDataService.CreateProductDataByProductVersions(request, azureAdB2C);

            if (productDetail.IsExchangeSetTooLarge)
            {
                Logger.LogError(EventIds.ExchangeSetTooLarge.ToEventId(), "Requested exchange set is too large for product versions endpoint for _X-Correlation-ID:{correlationId}.", GetCurrentCorrelationId());
                return BuildBadRequestErrorResponseForTooLargeExchangeSet();
            }
            Logger.LogInformation(EventIds.ESSPostProductVersionsRequestCompleted.ToEventId(), "Product Versions Endpoint Completed for BatchId:{batchId} and _X-Correlation-ID:{correlationId}", productDetail.BatchId, GetCurrentCorrelationId());

            return GetEssResponse(productDetail);
        }

        /// <summary>
        /// Provide all the releasable data after a datetime.
        /// </summary>
        /// <remarks>Given a datetime, build an Exchange Set of all the releasable ENC versions that have been issued since that datetime.</remarks>
        /// <param name="sinceDateTime" example="Wed, 21 Oct 2015 07:28:00 GMT" >The date and time from which changes are requested. Any changes since the date will be returned. The value should be the Last-Modified date returned by the last request to this operation. The date is in RFC 1123 format. The date and time must be within 28 days and cannot be in future.
        /// <br/><para><i>Example</i> : Wed, 21 Oct 2015 07:28:00 GMT</para>
        /// </param>
        /// <param name="callbackUri">An optional callback URI that will be used to notify the requestor once the requested Exchange Set is ready to download from the File Share Service. If not specified, then no call back notification will be sent.</param>
        /// <response code="200">A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set. If there are no updates since the sinceDateTime parameter, then a 'Not modified' response will be returned.</response>
        /// <response code="304">Not modified.</response>
        /// <response code="400">Bad Request.</response>
        /// <response code="401">Unauthorised - either you have not provided any credentials, or your credentials are not recognised.</response>
        /// <response code="403">Forbidden - you have been authorised, but you are not allowed to access this resource.</response>
        /// <response code="429">You have sent too many requests in a given amount of time. Please back-off for the time in the Retry-After header (in seconds) and try again.</response>
        /// <response code="500">Internal Server Error.</response>
        [HttpPost]
        [Route("/productData")]
        [Consumes("application/json")]
        [Produces("application/json")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.OK, name: "Last-Modified", type: "string", description: "Returns the date and time the file was last modified. The date format is in RFC 1123 format.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.OK, type: typeof(ExchangeSetResponse), description: "<p>A JSON body that indicates the URL that the Exchange Set will be available on as well as the number of cells in that Exchange Set.</p><p>If there are no updates since the sinceDateTime parameter, then a 'Not modified' response will be returned.</p>")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.NotModified, name: "Last-Modified", type: "string", description: "Returns the date and time the file was last modified. The date format is in RFC 1123 format.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.BadRequest, type: typeof(ErrorDescription), description: "Bad request.")]
        [SwaggerResponseHeader(statusCode: (int)HttpStatusCode.TooManyRequests, name: "Retry-After", type: "integer", description: "Specifies the time you should wait in seconds before retrying.")]
        [SwaggerResponse(statusCode: (int)HttpStatusCode.InternalServerError, type: typeof(InternalServerError), description: "Internal Server Error.")]
        public virtual async Task<IActionResult> GetProductDataSinceDateTime([FromQuery, SwaggerParameter(Required = true), SwaggerSchema(Format = "date-time")] string sinceDateTime,
            [FromQuery] string callbackUri)
        {
            Logger.LogInformation(EventIds.ESSGetProductsFromSpecificDateRequestStart.ToEventId(), "Product Data SinceDateTime Endpoint Started for _X-Correlation-ID:{correlationId}", GetCurrentCorrelationId());

            ProductDataSinceDateTimeRequest productDataSinceDateTimeRequest = new ProductDataSinceDateTimeRequest()
            {
                SinceDateTime = sinceDateTime,
                CallbackUri = callbackUri,
                CorrelationId = GetCurrentCorrelationId()
            };

            if (productDataSinceDateTimeRequest.SinceDateTime == null)
            {
                var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "sinceDateTime",
                        Description = "Query parameter 'sinceDateTime' is required."
                    }
                };
                return BuildBadRequestErrorResponse(error);
            }

            var validationResult = await productDataService.ValidateProductDataSinceDateTime(productDataSinceDateTimeRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out List<Error> errors))
            {
                return BuildBadRequestErrorResponse(errors);
            }
            AzureAdB2C azureAdB2C = new AzureAdB2C()
            {
                AudToken = TokenAudience,
                IssToken = TokenIssuer
            };

            var productDetail = await productDataService.CreateProductDataSinceDateTime(productDataSinceDateTimeRequest, azureAdB2C);

            if (productDetail.IsExchangeSetTooLarge)
            {
                Logger.LogError(EventIds.ExchangeSetTooLarge.ToEventId(), "Requested exchange set is too large for SinceDateTime endpoint for _X-Correlation-ID:{correlationId}.", GetCurrentCorrelationId());
                return BuildBadRequestErrorResponseForTooLargeExchangeSet();
            }
            Logger.LogInformation(EventIds.ESSGetProductsFromSpecificDateRequestCompleted.ToEventId(), "Product Data SinceDateTime Endpoint Completed for BatchId:{batchId} and _X-Correlation-ID:{correlationId}", productDetail.BatchId, GetCurrentCorrelationId());

            return GetEssResponse(productDetail);
        }

        
        [HttpPost]
        [Route("/clearSearchDownloadCacheData")]
        public virtual async Task<IActionResult> ClearSearchDownloadCacheData([FromBody] object request)
        {
            //Deserializing the request 
            var eventGridEvent = JsonConvert.DeserializeObject<CustomEventGridEvent[]>(request.ToString()).FirstOrDefault();
            var data = eventGridEvent.Data as JObject;
            var eventGridCacheDataRequest = (eventGridEvent.Data as JObject).ToObject<EventGridCacheDataRequest>();           
            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventStart.ToEventId(), "Clear Cache Event started for Data:{data} and _X-Correlation-ID:{correlationId}", JsonConvert.SerializeObject(data), eventGridCacheDataRequest, GetCurrentCorrelationId());
            ////Validation
            var validationResult = await productDataService.ValidateEventGridCacheDataRequest(eventGridCacheDataRequest);

            if (!validationResult.IsValid && validationResult.HasBadRequestErrors(out List<Error> errors))
            {
                return BuildBadRequestErrorResponse(errors);
            }
            ////Business logic
            var response = await productDataService.DeleteSearchAndDownloadCacheData(eventGridCacheDataRequest, GetCurrentCorrelationId());
            if (response)
            {
                Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event completed for ProductName:{} with OK response and _X-Correlation-ID:{correlationId}", eventGridCacheDataRequest.BatchId, GetCurrentCorrelationId());
                return Ok();
            }
            Logger.LogInformation(EventIds.ESSClearCacheSearchDownloadEventCompleted.ToEventId(), "Clear Cache Event returned with BadRequest response and response:{response} and _X-Correlation-ID:{correlationId}", response, GetCurrentCorrelationId());
            return BadRequest();
        }
    }
}