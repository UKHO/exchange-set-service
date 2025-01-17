// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public interface IAzureStorageBlobService
    {
        Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, V2SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetResponse);
    }
}
