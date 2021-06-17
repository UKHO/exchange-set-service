using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        public FulfilmentDataService(ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageService azureBlobStorageService, 
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    IOptions<EssFulfilmentStorageConfiguration> storageConfig, ILogger<FulfilmentDataService> logger)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.storageConfig = storageConfig;
            this.logger = logger;
        }

        public async Task<string> CreateExchangeSet(SalesCatalogueServiceResponseQueueMessage message)
        {            
            var fssFileName = $"{message.BatchId}-fssresponse.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            var response = await azureBlobStorageService.DownloadSalesCatalogueResponse(message.ScsResponseUri,message.CorrelationId);
            if (response.Products != null && response.Products.Any())
            {
                logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);
                var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(response?.Products,message.CorrelationId);
                logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", message.BatchId, message.CorrelationId);

                await fulfilmentFileShareService.UploadFileShareServiceData(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName);
            }

            return "Received Fulfilment Data Successfully!!!!";
        }
    }
}
