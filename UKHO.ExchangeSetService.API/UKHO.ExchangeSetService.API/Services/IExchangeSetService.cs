// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.Services
{
    public interface IExchangeSetService
    {
        Task<ServiceResponseResult<ExchangeSetResponse>> CreateExchangeSetByProductVersions(ProductDataProductVersionsRequest productVersionsRequest, CancellationToken cancellationToken);
    }
}
