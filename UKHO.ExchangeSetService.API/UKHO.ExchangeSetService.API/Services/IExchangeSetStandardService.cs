// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.V2.Response;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IExchangeSetStandardService
    {
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> CreateUpdatesSince(UpdatesSinceRequest updatesSinceRequest, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequest(ProductVersionsRequest productVersionsRequest, CancellationToken cancellationToken);
    }
}
