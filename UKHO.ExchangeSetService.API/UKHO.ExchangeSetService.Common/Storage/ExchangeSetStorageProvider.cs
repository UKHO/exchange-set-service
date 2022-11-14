using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Storage
{
    public class ExchangeSetStorageProvider : IExchangeSetStorageProvider
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly IAzureBlobStorageService azureBlobStorageService;
        public ExchangeSetStorageProvider(IOptions<EssFulfilmentStorageConfiguration> storageConfig,
            IAzureBlobStorageService azureBlobStorageService)
        {
            this.storageConfig = storageConfig;
            this.azureBlobStorageService = azureBlobStorageService;
        }

        public virtual async Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId, string expiryDate, DateTime scsRequestDateTime)
        {
            return await azureBlobStorageService.StoreSaleCatalogueServiceResponseAsync(storageConfig.Value.StorageContainerName, batchId, salesCatalogueResponse, callBackUri, correlationId, CancellationToken.None, expiryDate, scsRequestDateTime);
        }
    }
}
