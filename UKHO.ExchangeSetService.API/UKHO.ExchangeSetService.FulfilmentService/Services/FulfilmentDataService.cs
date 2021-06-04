using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IFulfilmentFileShareService fulfilmentFileShareService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly ILogger<FulfilmentDataService> logger;

        public FulfilmentDataService(ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageClient azureBlobStorageClient, 
                                    IFulfilmentFileShareService fulfilmentFileShareService,
                                    IOptions<EssFulfilmentStorageConfiguration> storageConfig, ILogger<FulfilmentDataService> logger)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.storageConfig = storageConfig;
            this.logger = logger;
        }

        public async Task<string> CreateExchangeSet(string uri, string batchid)
        {
            logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set web job started for {batchid}", batchid);
            var fssFileName = $"{batchid}-fssresponse.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            var response = await azureBlobStorageClient.DownloadSalesCatalogueResponse(uri);
            if (response.Products != null && response.Products.Any())
            {
                logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for {batchid}", batchid);
                var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(response?.Products);
                logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for {batchid}", batchid);

                await fulfilmentFileShareService.UploadFileShareServiceData(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName);
            }

            logger.LogInformation(EventIds.CreateExchangeSetRequestCompleted.ToEventId(), "Create Exchange Set web job completed for {batchid}", batchid);
            return "Received Fulfilment Data Successfully!!!!";
        }
    }
}
