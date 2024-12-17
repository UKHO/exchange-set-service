// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace UKHO.ExchangeSetService.API.Controllers
{
    [Route("v2/exchangeSet")]
    public class ExchangeSetController : ExchangeSetControllerBase<ExchangeSetController>
    {
        private readonly ILogger<ExchangeSetController> _logger;

        public ExchangeSetController(IHttpContextAccessor httpContextAccessor,ILogger<ExchangeSetController> logger) : base(httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    }
}
