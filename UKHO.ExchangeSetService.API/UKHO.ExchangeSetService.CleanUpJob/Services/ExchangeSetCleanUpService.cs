using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Configuration;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.CleanUpJob.Services
{
    public class ExchangeSetCleanUpService : IExchangeSetCleanUpService
    {
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IConfiguration configuration;
        private readonly ILogger<ExchangeSetCleanUpService> logger;
        private readonly IOptions<CleanUpConfig> cleanUpConfig;

        public ExchangeSetCleanUpService(IAzureBlobStorageClient azureBlobStorageClient,
                              IOptions<EssFulfilmentStorageConfiguration> storageConfig,
                              ISalesCatalogueStorageService scsStorageService,
                              IConfiguration configuration,
                              ILogger<ExchangeSetCleanUpService> logger,
                              IOptions<CleanUpConfig> cleanUpConfig)
        {
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.storageConfig = storageConfig;
            this.scsStorageService = scsStorageService;
            this.configuration = configuration;
            this.logger = logger;
            this.cleanUpConfig = cleanUpConfig;
        }
        public async Task<bool> DeleteHistoricFoldersAndFiles()
        {
            string homeDirectoryPath = configuration["HOME"];
            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            logger.LogInformation(EventIds.DeleteHistoricFoldersAndFilesStarted.ToEventId(), "Clean up process of historic folders started.");

            var response = await azureBlobStorageClient.DeleteDirectoryAsync(cleanUpConfig.Value.NumberOfDay, storageAccountConnectionString, storageConfig.Value.StorageContainerName, homeDirectoryPath);
            if (response)
            {
                logger.LogInformation(EventIds.DeleteHistoricFoldersAndFilesCompleted.ToEventId(), "Clean up process of historic folders completed.");
                return true;
            }
            else
            {
                logger.LogError(EventIds.DeleteHistoricFoldersAndFilesFailed.ToEventId(), "Clean up process of historic folders failed.");
                return false;
            }
        }
    }
}
