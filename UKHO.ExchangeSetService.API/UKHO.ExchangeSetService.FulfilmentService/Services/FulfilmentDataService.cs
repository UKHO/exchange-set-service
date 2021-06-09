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

        public async Task<string> CreateExchangeSet(string uri, string batchid)
        {            
            var fssFileName = $"{batchid}-fssresponse.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            var response = await azureBlobStorageService.DownloadSalesCatalogueResponse(uri);
            if (response.Products != null && response.Products.Any())
            {
                logger.LogInformation(EventIds.QueryFileShareServiceRequestStart.ToEventId(), "Query File share service request started for {batchid}", batchid);
                var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(response?.Products);
                logger.LogInformation(EventIds.QueryFileShareServiceRequestCompleted.ToEventId(), "Query File share service request completed for {batchid}", batchid);

                await fulfilmentFileShareService.UploadFileShareServiceData(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName);
            }

            return "Received Fulfilment Data Successfully!!!!";
        }
    }
}
