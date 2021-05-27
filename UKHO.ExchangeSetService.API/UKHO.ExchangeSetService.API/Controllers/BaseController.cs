using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Models.Response;


namespace UKHO.ExchangeSetService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        protected readonly ILogger<T> Logger;
        public const string LastModifiedDateHeaderKey = "Last-Modified";
        public const string InternalServerError = "Internal Server Error";
        public const string NotModified = "Not Modified";
        protected BaseController(IHttpContextAccessor httpContextAccessor, ILogger<T> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            Logger = logger;
        }

        protected string GetCurrentCorrelationId()
        {
            return httpContextAccessor.HttpContext.Request.Headers[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault();
        }
        protected IActionResult BuildBadRequestErrorResponse(List<Error> errors)
        {
            return new BadRequestObjectResult(new ErrorDescription
            {
                Errors = errors,
                CorrelationId = GetCurrentCorrelationId()
            });
        }

        protected IActionResult BuildInternalServerErrorResponse()
        {
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
            httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified);
            return new StatusCodeResult(StatusCodes.Status304NotModified);
        }

        private IActionResult BuildOkResponse(ExchangeSetServiceResponse model)
        {
            if (model.LastModified != null)
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, model.LastModified);
            return Ok(model.ExchangeSetResponse);
        }
    }
}