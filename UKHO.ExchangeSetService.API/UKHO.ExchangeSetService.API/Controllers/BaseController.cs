using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [ExcludeFromCodeCoverage]
    public abstract class BaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        protected readonly ILogger<T> Logger;       
       
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

    }
}