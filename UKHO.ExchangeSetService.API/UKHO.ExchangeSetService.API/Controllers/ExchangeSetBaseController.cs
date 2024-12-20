// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace UKHO.ExchangeSetService.API.Controllers
{
    public abstract class ExchangeSetBaseController<T> : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public const string XCorrelationIdHeaderKey = "X-Correlation-ID";

        protected ExchangeSetBaseController(IHttpContextAccessor httpContextAccessor)
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

        /// <summary>
        /// Get Request Cancellation Token.
        /// </summary>
        /// <remarks>
        /// Cancellation Token can be found in request.
        /// If Cancellation Token is true, Then notifies the underlying connection is aborted thus request operations should be cancelled.
        /// </remarks>
        /// <returns>Cancellation Token</returns>
        protected CancellationToken GetRequestCancellationToken()
        {
            return _httpContextAccessor.HttpContext.RequestAborted;
        }
    }
}
