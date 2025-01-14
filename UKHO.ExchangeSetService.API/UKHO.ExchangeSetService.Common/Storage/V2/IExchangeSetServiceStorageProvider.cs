// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Storage.V2
{
    public interface IExchangeSetServiceStorageProvider
    {
        Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetResponse);
    }
}
