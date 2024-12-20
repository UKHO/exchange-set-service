// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceResponseResultExtensions
    {
        public const string LastModifiedDateHeaderKey = "Last-Modified";

        public static IActionResult ToActionResult<T>(this ServiceResponseResult<T> result, IHttpContextAccessor httpContextAccessor)
        {
            var exchangeSetServiceResponse = result.Value as ExchangeSetStandardServiceResponse ?? new ExchangeSetStandardServiceResponse();

            if (!string.IsNullOrWhiteSpace(exchangeSetServiceResponse.LastModified))
            {
                httpContextAccessor.HttpContext.Response.Headers.Add(LastModifiedDateHeaderKey, exchangeSetServiceResponse.LastModified);
            }

            return result.StatusCode switch
            {
                HttpStatusCode.OK => new OkObjectResult(result.Value) { StatusCode = (int)HttpStatusCode.OK },
                HttpStatusCode.Accepted => new ObjectResult(exchangeSetServiceResponse.ExchangeSetResponse) { StatusCode = (int)HttpStatusCode.Accepted },
                HttpStatusCode.BadRequest => new BadRequestObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.NotFound => new NotFoundObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.NoContent => new NoContentResult(),
                _ => new StatusCodeResult((int)result.StatusCode)
            };
        }
    }
}
