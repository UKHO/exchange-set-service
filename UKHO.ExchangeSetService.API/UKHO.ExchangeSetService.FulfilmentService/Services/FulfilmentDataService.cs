using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public class FulfilmentDataService : IFulfilmentDataService
    {
        private readonly IScsStorageService scsStorageService;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;

        public FulfilmentDataService(IScsStorageService scsStorageService, IAzureBlobStorageClient azureBlobStorageClient,
                                     IOptions<EssFulfilmentStorageConfiguration> storageConfig)
        {
            this.scsStorageService = scsStorageService;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.storageConfig = storageConfig;
        }

        public async Task<string> DownloadSalesCatalogueResponse(string ScsResponseUri, string batchid)
        {
            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
            CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlob(batchid + ".json", storageAccountConnectionString, storageConfig.Value.StorageContainerName);

            var responseFile = await cloudBlockBlob.DownloadTextAsync();
            var salesCatalogueResponse = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(responseFile);
            Console.WriteLine("Download completed Successfully!!!!");
            return "Download completed Successfully!!!!";

        }
    }
}
