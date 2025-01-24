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

        /// <summary>
        /// Saves the sales catalogue storage details to Azure Blob Storage.
        /// </summary>
        /// <param name="salesCatalogueResponse">The sales catalogue product response.</param>
        /// <param name="batchId">The batch identifier.</param>
        /// <param name="callBackUri">The callback URI.</param>
        /// <param name="exchangeSetStandard">The exchange set standard.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="expiryDate">The expiry date.</param>
        /// <param name="scsRequestDateTime">The SCS request date and time.</param>
        /// <param name="isEmptyExchangeSet">Indicates if the exchange set is empty.</param>
        /// <param name="exchangeSetResponse">The exchange set standard response.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <param name="productIdentifier">The product identifier (optional).</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating success or failure.</returns>
        public virtual async Task<bool> SaveSalesCatalogueStorageDetails(V2SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetResponse, ApiVersion apiVersion, string productIdentifier = "")
        {
            return await _azureStorageBlobService.StoreSalesCatalogueServiceResponseAsync(_storageConfig.Value.ExchangeSetStorageContainerName, batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, CancellationToken.None, expiryDate, scsRequestDateTime, isEmptyExchangeSet, exchangeSetResponse, apiVersion, productIdentifier);
        }
    }
}
