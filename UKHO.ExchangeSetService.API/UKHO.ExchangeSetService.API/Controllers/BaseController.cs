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
        public class InternalServerErrorObjectResult : ObjectResult
        {
            public InternalServerErrorObjectResult(object value) : base(value)
            {
                StatusCode = StatusCodes.Status500InternalServerError;
            }
        }

        protected IActionResult BuildInternalServerErrorResponse()
        {           
            return new InternalServerErrorObjectResult(new InternalServerError
            {
               CorrelationId = GetCurrentCorrelationId(),
               Detail = InternalServerError,
            });
        }

        public class NotModifiedObjectResult : ObjectResult
        {
            public NotModifiedObjectResult(object value) : base(value)
            {
                StatusCode = StatusCodes.Status304NotModified;
            }
        }

        protected IActionResult BuildNotModifiedResponse()
        {            
            return new NotModifiedObjectResult(new ErrorDescription {              
            });
        }

        protected IActionResult GetEssResponse(ExchangeSetServiceResponse model, List<Error> errors = null, HttpContext context= null)
        {
            HttpStatusCode code = model.HttpstatusCode;
           
            switch (code)
            {
                case (HttpStatusCode)(int)HttpStatusCode.OK:
                    return Ok(model.ExchangeSetResponse);
                case (HttpStatusCode)(int)HttpStatusCode.InternalServerError:
                    return BuildInternalServerErrorResponse();                    
                case (HttpStatusCode)(int)HttpStatusCode.BadRequest:
                    return BuildBadRequestErrorResponse(errors);
                case (HttpStatusCode)(int)HttpStatusCode.NotModified:
                    httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey,model.LastModified);
                    return BuildNotModifiedResponse();
                default:
                   return BuildInternalServerErrorResponse();
            }
        }

    }
}