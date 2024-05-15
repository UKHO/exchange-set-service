using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Security.Claims;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;


namespace UKHO.ExchangeSetService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        protected readonly ILogger<T> Logger;
        protected new HttpContext HttpContext => httpContextAccessor.HttpContext;
        public const string LastModifiedDateHeaderKey = "Last-Modified";
        public const string InternalServerError = "Internal Server Error";
        public const string NotModified = "Not Modified";
        protected string TokenAudience => httpContextAccessor.HttpContext.User.FindFirstValue("aud");
        protected string TokenIssuer => httpContextAccessor.HttpContext.User.FindFirstValue("iss");

        protected BaseController(IHttpContextAccessor httpContextAccessor, ILogger<T> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            Logger = logger;
        }

        protected string GetCurrentCorrelationId()
        {
            var correlationId = httpContextAccessor.HttpContext.Request.Headers[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault();
            if (Guid.TryParse(correlationId, out Guid correlationIdGuid))
            {
                correlationId = correlationIdGuid.ToString();
            }
            else
            {
                correlationId = Guid.Empty.ToString();
                LogError(EventIds.BadRequest.ToEventId(), null, "_X-Correlation-ID is invalid", correlationId);
            }
            return correlationId;
        }

        protected IActionResult BuildBadRequestErrorResponse(List<Error> errors)
        {
            LogError(EventIds.BadRequest.ToEventId(), errors, "BadRequest", GetCurrentCorrelationId());

            return new BadRequestObjectResult(new ErrorDescription
            {
                Errors = errors,
                CorrelationId = GetCurrentCorrelationId()
            });
        }

        protected IActionResult BuildBadRequestErrorResponseForTooLargeExchangeSet()
        {
            var error = new List<Error>
                {
                    new Error()
                    {
                        Source = "exchangeSetSize",
                        Description = "The Exchange Set requested is very large and will not be created, please use a standard Exchange Set provided by the UKHO."
                    }
                };
            return BuildBadRequestErrorResponse(error);
        }

        protected IActionResult BuildInternalServerErrorResponse()
        {
            LogError(EventIds.InternalServerError.ToEventId(), null, "InternalServerError", GetCurrentCorrelationId());

            var objectResult = new ObjectResult
                (new InternalServerError
                {
                    CorrelationId = GetCurrentCorrelationId(),
                    Detail = InternalServerError,
                });
            objectResult.StatusCode = StatusCodes.Status500InternalServerError;
            return objectResult;
        }

        protected IActionResult GetEssResponse(ExchangeSetServiceResponse model, List<Error> errors = null)
        {
            switch (model.HttpStatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Created:
                    return BuildOkResponse(model);

                case HttpStatusCode.InternalServerError:
                    return BuildInternalServerErrorResponse();

                case HttpStatusCode.BadRequest:
                    return BuildBadRequestErrorResponse(errors);

                case HttpStatusCode.NotModified:
                    return BuildNotModifiedResponse(model);

                default:
                    return BuildInternalServerErrorResponse();
            }
        }

        private IActionResult BuildNotModifiedResponse(ExchangeSetServiceResponse model)
        {
            LogInfo(EventIds.NotModified.ToEventId(), "NotModified", GetCurrentCorrelationId());
            if (model.LastModified != null)
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified);
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        private IActionResult BuildOkResponse(ExchangeSetServiceResponse model)
        {
            if (model.LastModified != null)
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified);
            return Ok(model.ExchangeSetResponse);
        }

        protected IActionResult GetCacheResponse()
        {
            return new OkObjectResult(StatusCodes.Status200OK);
        }

        private void LogError(EventId eventId, List<Error> errors, string errorType, string correlationId)
        {
            Logger.LogError(eventId, $"{HttpContext.Request.Path} - {errorType} - {{Errors}} for CorrelationId - {{correlationId}}", errors, correlationId);
        }

        private void LogInfo(EventId eventId, string infoType, string correlationId)
        {
            Logger.LogInformation(eventId, $"{HttpContext.Request.Path} - {infoType} - for CorrelationId - {{correlationId}}", correlationId);
        }
        protected IActionResult GetScsResponse(SalesCatalogueResponse model, List<Error> errors = null)
        {
            switch (model.ResponseCode)
            {
                case HttpStatusCode.OK:
                    return BuildOkScsResponse(model);

                case HttpStatusCode.InternalServerError:
                    return BuildInternalServerErrorResponse();

                case HttpStatusCode.BadRequest:
                    return BuildBadRequestErrorResponse(errors);

                case HttpStatusCode.NotModified:
                    return BuildNotModifiedScsResponse(model);

                default:
                    return BuildInternalServerErrorResponse();
            }
        }

        private IActionResult BuildOkScsResponse(SalesCatalogueResponse model)
        {
            if (model.LastModified != null)
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified.ToString());
            return Ok(model.ResponseBody);
        }

        private IActionResult BuildNotModifiedScsResponse(SalesCatalogueResponse model)
        {
            LogInfo(EventIds.NotModified.ToEventId(), "NotModified", GetCurrentCorrelationId());
            if (model.LastModified != null)
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified.ToString());
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }
    }
}