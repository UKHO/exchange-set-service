using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
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

        public FulfilmentDataService(IAzureBlobStorageService azureBlobStorageService, 
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    ILogger<FulfilmentDataService> logger,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
        {
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.logger = logger;
            this.fileShareServiceConfig = fileShareServiceConfig;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {
            var response = await DownloadSalesCatalogueResponse(message);
            if (response.Products != null && response.Products.Any())
            {
                var productsList = ConfigHelper.SplitList((response.Products), fileShareServiceConfig.Value.ParallelSearchTaskCount);
                var tasks = productsList.Select(async item =>
                {
                    await QueryAndDownloadFileShareServiceFiles(message, item);
                });
                await Task.WhenAll(tasks);
            }

            return "Received Fulfilment Data Successfully!!!!";
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(SalesCatalogueServiceResponseQueueMessage message)
        {
            return await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri);
        }

        public async Task QueryAndDownloadFileShareServiceFiles(SalesCatalogueServiceResponseQueueMessage message, List<Products> products)
        {
            logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for {BatchId}", message.BatchId);
            var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(products);
            logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for {BatchId}", message.BatchId);

            if (searchBatchResponse != null && searchBatchResponse.Any())
            {
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesStart.ToEventId(), "Download File share service request started for {BatchId}", message.BatchId);
                await fulfilmentFileShareService.DownloadFileShareServiceFiles(message, searchBatchResponse);
                logger.LogInformation(EventIds.DownloadFileShareServiceFilesCompleted.ToEventId(), "Download File share service request completed for {BatchId}", message.BatchId);
            }
        }
    }
}
