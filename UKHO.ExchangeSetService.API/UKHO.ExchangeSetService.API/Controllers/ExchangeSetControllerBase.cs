using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UKHO.ExchangeSetService.API.Controllers
{
    public abstract class ExchangeSetControllerBase<T> : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        protected ExchangeSetControllerBase(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get Correlation Id.
        /// </summary>
        /// <remarks>
        /// Correlation Id is Guid based id to track request.
        /// Correlation Id can be found in request headers.
        /// </remarks>
        /// <returns>Correlation Id</returns>
        protected string GetCorrelationId()
        {
            return _httpContextAccessor.HttpContext!.Request.Headers[XCorrelationIdHeaderKey].FirstOrDefault()!;
        }
    }
}
