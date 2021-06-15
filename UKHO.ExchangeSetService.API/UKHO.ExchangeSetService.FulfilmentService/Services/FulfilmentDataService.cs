using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private readonly IFulfilmentFileShareService fulfilmentFileShareService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly ILogger<FulfilmentDataService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;        
        private readonly IConfiguration configuration;        

        public FulfilmentDataService(ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageService azureBlobStorageService, 
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    IOptions<EssFulfilmentStorageConfiguration> storageConfig,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.storageConfig = storageConfig;      
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.configuration = configuration;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetRootPath = Path.Combine(homeDirectoryPath, DateTime.UtcNow.ToString("ddMMMyyyy"), message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder, fileShareServiceConfig.Value.EncRoot);
            var fssFileName = $"{message.BatchId}-fssresponse.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            var response = await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri);
            if (response.Products != null && response.Products.Any())
            {
                logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for {BatchId}", message.BatchId);
                var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(response?.Products);
                logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for {BatchId}", message.BatchId);
                await fulfilmentFileShareService.UploadFileShareServiceData(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName);
            }
            await CreateAncillaryFiles(message.BatchId, exchangeSetRootPath);          
            return "Received Fulfilment Data Successfully!!!!";
        }
        private async Task CreateAncillaryFiles(string batchId, string exchangeSetRootPath)
        {
           await DownloadReadMeFile(batchId, exchangeSetRootPath);
        }      

        public async Task DownloadReadMeFile(string batchId, string exchangeSetRootPath)
        {
            logger.LogInformation(EventIds.SearchDownloadReadMeFileRequestStart.ToEventId(), "Search and download ReadMe Text File start for {BatchId}", batchId);
            
            string readMeFilePath = await fulfilmentFileShareService.SearchReadMeFilePath(batchId);            
            if (!string.IsNullOrWhiteSpace(readMeFilePath))
               await fulfilmentFileShareService.DownloadReadMeFile(readMeFilePath, batchId, exchangeSetRootPath);
            
            logger.LogInformation(EventIds.SearchDownloadReadMeFileRequestCompleted.ToEventId(), "Search and download ReadMe Text File completed for {BatchId}", batchId);
        }
    }
}
