using Azure.Storage;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Storage
{
    public class SalesCatalogueStorageService : ISalesCatalogueStorageService
    {
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        public SalesCatalogueStorageService(IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.storageConfig = storageConfig;
        }

        public string GetStorageAccountConnectionString(string storageAccountName = null, string storageAccountKey = null)
        {
            string ScsStorageAccountAccessKeyValue = !string.IsNullOrEmpty(storageAccountKey) ? storageAccountKey : storageConfig.Value.StorageAccountKey;
            string ScsStorageAccountName = !string.IsNullOrEmpty(storageAccountName) ? storageAccountName : storageConfig.Value.StorageAccountName;

            if (string.IsNullOrWhiteSpace(ScsStorageAccountAccessKeyValue))
            {
                throw new KeyNotFoundException($"Storage account accesskey not found");
            }

            string storageAccountConnectionString = $"DefaultEndpointsProtocol=https;AccountName={ScsStorageAccountName};AccountKey={ScsStorageAccountAccessKeyValue};EndpointSuffix=core.windows.net";

            return storageAccountConnectionString;
        }

        public StorageSharedKeyCredential GetStorageSharedKeyCredentials()
        {
            var config = storageConfig.Value;

            if (config == null || string.IsNullOrWhiteSpace(config.StorageAccountName) || string.IsNullOrWhiteSpace(config.StorageAccountKey))
            {
                throw new KeyNotFoundException("Storage account credentials missing from config");
            }

            return new StorageSharedKeyCredential(config.StorageAccountName, config.StorageAccountKey);
        }
    }
}
