﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services.V2
{
    public interface IExchangeSetStandardService
    {
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequest(string[] productNames, string callbackUri, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequest(IEnumerable<ProductVersionRequest> productVersionRequest, string callbackUri, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequest(UpdatesSinceRequest updatesSinceRequest, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken);
    }
}