// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers.V2;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Storage.V2
{
    public class ExchangeSetServiceStorageProvider : IExchangeSetServiceStorageProvider
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> _storageConfig;
        private readonly IAzureStorageBlobService _azureStorageBlobService;

        public ExchangeSetServiceStorageProvider(IOptions<EssFulfilmentStorageConfiguration> storageConfig,
            IAzureStorageBlobService azureStorageBlobService)
        {
            _storageConfig = storageConfig?? throw new ArgumentNullException(nameof(storageConfig));
            _azureStorageBlobService = azureStorageBlobService ?? throw new ArgumentNullException(nameof(azureStorageBlobService)); ;
        }

        public virtual async Task<bool> SaveSalesCatalogueStorageDetails(V2SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetResponse, ApiVersion apiVersion, string productIdentifier = "")
        {
            return await _azureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(_storageConfig.Value.ExchangeSetStorageContainerName, batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, CancellationToken.None, expiryDate, scsRequestDateTime, isEmptyExchangeSet, exchangeSetResponse, apiVersion,productIdentifier);
        }
    }
}
