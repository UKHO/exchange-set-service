// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public interface ISalesCatalogueService
    {
        Task<ServiceResponseResult<SalesCatalogueResponse>> PostProductNamesAsync(ApiVersion apiVersion, string exchangeSetStandard, IEnumerable<string> productNames, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<SalesCatalogueResponse>> PostProductVersionsAsync(ApiVersion apiVersion, string exchangeSetStandard, IEnumerable<ProductVersionRequest> productVersions, string correlationId, CancellationToken cancellationToken);
        Task<ServiceResponseResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(ApiVersion apiVersion, string exchangeSetStandard, UpdatesSinceRequest updatesSinceRequest, string correlationId, CancellationToken cancellationToken);
    }
}
