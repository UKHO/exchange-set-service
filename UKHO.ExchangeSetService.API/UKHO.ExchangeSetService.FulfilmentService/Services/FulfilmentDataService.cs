using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private readonly IFulfilmentFileShareService fulfilmentFileShareService;
        private readonly ILogger<FulfilmentDataService> logger;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IConfiguration configuration;

        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService, 
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IConfiguration configuration)
        {
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.configuration = configuration;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {
            string homeDirectoryPath = configuration["HOME"];
            var exchangeSetRootPath = Path.Combine(homeDirectoryPath, DateTime.UtcNow.ToString("ddMMMyyyy"), message.BatchId, fileShareServiceConfig.Value.ExchangeSetFileFolder, fileShareServiceConfig.Value.EncRoot);

            var response = await DownloadSalesCatalogueResponse(message);
            if (response.Products != null && response.Products.Any())
            {
                int allowedThreadPerEs = fileShareServiceConfig.Value.ParallelSearchTaskCount;
                int pgGroupCount = response.Products.Count % allowedThreadPerEs == 0 ? response.Products.Count / allowedThreadPerEs : (response.Products.Count / allowedThreadPerEs) + 1;
                var productsList = ConfigHelper.SplitList((response.Products), pgGroupCount);

                var tasks = productsList.Select(async item =>
                {
                    await QueryAndDownloadFileShareServiceFiles(message, item, exchangeSetRootPath);
                });
                await Task.WhenAll(tasks);
            }

            return "Received Fulfilment Data Successfully!!!!";
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri);
        }

        public async Task<bool> QueryAndDownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products, string exchangeSetRootPath)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for {BatchId}", message.BatchId);
            var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(products);
            logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for {BatchId}", message.BatchId);

            if (searchBatchResponse != null && searchBatchResponse.Any())
            {
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesStart.ToEventId(), "Download File share service request started for {BatchId}", message.BatchId);
                await fulfilmentFileShareService.DownloadFileShareServiceFiles(message, searchBatchResponse, exchangeSetRootPath);
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesCompleted.ToEventId(), "Download File share service request completed for {BatchId}", message.BatchId);
                return true;
            }
            return false;
        }
    }
}
