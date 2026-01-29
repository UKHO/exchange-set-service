using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
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

        public virtual async Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string exchangeSetStandard, string correlationId, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse, string exchangeSetLayout)
        {
            return await azureBlobStorageService.StoreSaleCatalogueServiceResponseAsync(storageConfig.Value.StorageContainerName, batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, CancellationToken.None, expiryDate, scsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetResponse, exchangeSetLayout);
        }
    }
}
