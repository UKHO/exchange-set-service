using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IQueryFssService queryFssService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;

        public FulfilmentDataService(ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageClient azureBlobStorageClient, IQueryFssService queryFssService,
                                     IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.queryFssService = queryFssService;
            this.storageConfig = storageConfig;
        }

        public async Task<string> BuildExchangeSet(string batchid)
        {
            var fssFileName = $"{batchid}-fssresponse.json";
            var scsFileName = $"{batchid}.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            SalesCatalogueResponse salesCatalogueResponse = await azureBlobStorageClient.DownloadScsResponse(scsFileName);
            if (salesCatalogueResponse?.ResponseBody?.Products != null && salesCatalogueResponse.ResponseBody.Products.Any())
            {
                var searchBatchResponse = await queryFssService.QueryFss(salesCatalogueResponse.ResponseBody.Products);
                await queryFssService.UploadFssDataToBlob(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName); 
            }
            return "Received Fulfilment Data Successfully!!!!";

        }
    }
}
