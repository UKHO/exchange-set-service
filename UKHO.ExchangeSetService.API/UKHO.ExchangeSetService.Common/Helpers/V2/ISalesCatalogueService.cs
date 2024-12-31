// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Request;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public interface ISalesCatalogueService
    {
        Task<ServiceResponseResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string exchangeSetStandard, string sinceDateTime, string correlationId);

        Task<ServiceResponseResult<SalesCatalogueResponse>> PostProductVersionsAsync(IEnumerable<ProductVersionRequest> productVersions, string exchangeSetStandard, string correlationId);
    }
}
