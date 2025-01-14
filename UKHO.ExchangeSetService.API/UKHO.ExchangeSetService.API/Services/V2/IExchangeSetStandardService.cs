// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.V2.Request;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.API.Services.V2
{
    public interface IExchangeSetStandardService
    {
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductNamesRequestAsync(string[] productNames, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessProductVersionsRequestAsync(IEnumerable<ProductVersionRequest> productVersionRequest, ApiVersion apiVersion, string exchangeSetStandard, string callbackUri, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<ExchangeSetStandardServiceResponse>> ProcessUpdatesSinceRequestAsync(UpdatesSinceRequest updatesSinceRequest, string exchangeSetStandard, string productIdentifier, string callbackUri, string correlationId, CancellationToken cancellationToken);
    }
}
