// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Mvc;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ControllerBase
    {
        [HttpPost("{standard}/productIdentifiers")]
        public IActionResult ProductIdentifiers(ExchangeSetStandard exchangeSetStandard, [FromBody] string[] productIdentifiers, [FromQuery] string callbackUri)
        {
            var result = ExchangeSetServiceResponseResult<ExchangeSetResponse>.Success(new ExchangeSetResponse()); // This is a placeholder, the actual implementation is not provided
            return result.StatusCode switch
            {
                HttpStatusCode.OK => Ok(result.Value),
                HttpStatusCode.BadRequest => BadRequest(result.ErrorResponse),
                _ => StatusCode((int)result.StatusCode)
            };
        }
    }
}
