using Microsoft.Extensions.Options;
using System.Collections.Generic;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Storage
{
    public class SalesCatalogueServiceStorageService : ISalesCatalogueStorageService
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        public SalesCatalogueServiceStorageService(IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.storageConfig = storageConfig;
        }

        public string GetStorageAccountConnectionString()
        {
            string ScsStorageAccountAccessKeyValue = storageConfig.Value.StorageAccountKey;

            if (string.IsNullOrWhiteSpace(ScsStorageAccountAccessKeyValue))
            {
                throw new KeyNotFoundException($"Storage account accesskey not found");
            }

            string storageAccountConnectionString = $"DefaultEndpointsProtocol=https;AccountName={storageConfig.Value.StorageAccountName};AccountKey={ScsStorageAccountAccessKeyValue};EndpointSuffix=core.windows.net";

            return storageAccountConnectionString;
        }
    }
}
