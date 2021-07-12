using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.CleanUpJob.Services
{
    public class ExchangeSetCleanUpService : IExchangeSetCleanUpService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IConfiguration configuration;

        public ExchangeSetCleanUpService(IAzureBlobStorageClient azureBlobStorageClient,
                              IOptions<EssFulfilmentStorageConfiguration> storageConfig,
                              ISalesCatalogueStorageService scsStorageService,
                              IConfiguration configuration)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.storageConfig = storageConfig;
            this.scsStorageService = scsStorageService;
            this.configuration = configuration;
        }
        public async Task<bool> CleanUpFoldersFiles()
        {
            string homeDirectoryPath = configuration["HOME"];
            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
           
            var response = await azureBlobStorageClient.DeleteDirectoryAsync(storageAccountConnectionString, storageConfig.Value.StorageContainerName, homeDirectoryPath);
            if (response)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
