using Microsoft.Extensions.Options;
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
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        public ExchangeSetStorageProvider(IOptions<EssFulfilmentStorageConfiguration> storageConfig,
            IAzureBlobStorageClient azureBlobStorageClient)
        {
            this.storageConfig = storageConfig;
            this.azureBlobStorageClient = azureBlobStorageClient;
        }

        public virtual async Task<bool> SaveSalesCatalogueStorageDetails(SalesCatalogueProductResponse salesCatalogueResponse, string batchId, string callBackUri, string correlationId)
        {
            return await azureBlobStorageClient.StoreSaleCatalogueServiceResponseAsync(storageConfig.Value.StorageContainerName, batchId, salesCatalogueResponse, callBackUri, correlationId, CancellationToken.None);
        }

    }
}
