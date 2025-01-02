// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceResponseResultExtensions
    {
        public const string LastModifiedDateHeaderKey = "Last-Modified";
        public const string InternalServerError = "Internal Server Error";

        public static IActionResult ToActionResult<T>(this ServiceResponseResult<T> result, IHttpContextAccessor httpContextAccessor, string correlationId)
        {
            var exchangeSetServiceResponse = result.Value as ExchangeSetStandardServiceResponse ?? new ExchangeSetStandardServiceResponse();

            if (!string.IsNullOrWhiteSpace(exchangeSetServiceResponse.LastModified))
            {
                httpContextAccessor.HttpContext.Response.Headers.Append(LastModifiedDateHeaderKey, exchangeSetServiceResponse.LastModified);
            }

            return result.StatusCode switch
            {
                HttpStatusCode.OK => new OkObjectResult(result.Value) { StatusCode = StatusCodes.Status200OK },
                HttpStatusCode.Accepted => new AcceptedResult(string.Empty, exchangeSetServiceResponse.ExchangeSetStandardResponse) { StatusCode = StatusCodes.Status202Accepted },
                HttpStatusCode.NoContent => new NoContentResult(),
                HttpStatusCode.NotModified => new StatusCodeResult(StatusCodes.Status304NotModified),
                HttpStatusCode.BadRequest => new BadRequestObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.NotFound => new NotFoundObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.InternalServerError => new ObjectResult(new InternalServerError
                {
                    CorrelationId = correlationId,
                    Detail = InternalServerError,
                })
                { StatusCode = StatusCodes.Status500InternalServerError },
                _ => new StatusCodeResult((int)result.StatusCode)
            };
        }
    }
}
