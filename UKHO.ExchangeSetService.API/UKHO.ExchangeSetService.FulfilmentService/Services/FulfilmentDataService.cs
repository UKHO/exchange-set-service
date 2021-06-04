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
        private readonly IFulfilmentFileShareService fulfilmentFileShareService;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;

        public FulfilmentDataService(ISalesCatalogueStorageService scsStorageService, IAzureBlobStorageClient azureBlobStorageClient, IFulfilmentFileShareService fulfilmentFileShareService,
                                     IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.fulfilmentFileShareService = fulfilmentFileShareService;
            this.storageConfig = storageConfig;
        }

        public async Task<string> CreateExchangeSet(string uri, string batchid)
        {
            var fssFileName = $"{batchid}-fssresponse.json";

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();

            var response = await azureBlobStorageClient.DownloadSalesCatalogueResponse(uri);
            if (response.Products != null && response.Products.Any())
            {
                var searchBatchResponse = await fulfilmentFileShareService.QueryFileShareServiceData(response?.Products);
                await fulfilmentFileShareService.UploadFileShareServiceData(fssFileName, searchBatchResponse, storageAccountConnectionString, storageConfig.Value.StorageContainerName);
            }
            return "Received Fulfilment Data Successfully!!!!";

        }
    }
}
