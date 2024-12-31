// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public interface ISalesCatalogueService
    {
        Task<ServiceResponseResult<SalesCatalogueResponse>> GetProductsFromSpecificDateAsync(string exchangeSetStandard, string sinceDateTime, string correlationId);
    }
}
