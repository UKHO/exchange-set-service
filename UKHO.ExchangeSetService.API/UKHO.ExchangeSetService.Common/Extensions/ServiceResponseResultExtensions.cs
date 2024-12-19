// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using UKHO.ExchangeSetService.Common.Models;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class ServiceResponseResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ServiceResponseResult<T> result)
        {
            return result.StatusCode switch
            {
                HttpStatusCode.OK => new OkObjectResult(result.Value) { StatusCode = (int)HttpStatusCode.OK },
                HttpStatusCode.Accepted => new ObjectResult(result.Value) { StatusCode = (int)HttpStatusCode.Accepted },
                HttpStatusCode.BadRequest => new BadRequestObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.NotFound => new NotFoundObjectResult(result.ErrorDescription.Errors),
                HttpStatusCode.NoContent => new NoContentResult(),
                _ => new StatusCodeResult((int)result.StatusCode)
            };
        }
    }
}
