using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class AzureBlobStorageClient : IAzureBlobStorageClient
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private const string CONTENT_TYPE = "application/json";
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly IAzureMessageQueueHelper azureMessageQueueHelper;
        private readonly ILogger<AzureBlobStorageClient> logger;
        public AzureBlobStorageClient(ISalesCatalogueStorageService scsStorageService, IOptions<EssFulfilmentStorageConfiguration> storageConfig, IAzureMessageQueueHelper azureMessageQueueHelper, ILogger<AzureBlobStorageClient> logger)
        {
            this.scsStorageService = scsStorageService;
            this.storageConfig = storageConfig;
            this.azureMessageQueueHelper = azureMessageQueueHelper;
            this.logger = logger;
        }

        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CancellationToken cancellationToken)
        {
            string uploadFileName = string.Concat(batchId, ".json");

            string storageAccountConnectionString =
                  scsStorageService.GetStorageAccountConnectionString();
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            await UploadSalesCatalogueServiceResponseToBlobAsync(cloudBlockBlob, salesCatalogueResponse);

            await AddQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob);

            logger.LogInformation(EventIds.SCSResponseStoredAndSentMessageInQueue.ToEventId(), "Sales catalogue response saved for the {batchId}", batchId);
            return true;
        }

        public async Task AddQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob)
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetSalesCatalogueServiceResponseQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob);
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(scsResponseQueueMessage);
            await azureMessageQueueHelper.AddMessage(storageConfig.Value, scsResponseQueueMessageJSON);
        }

        public async Task UploadSalesCatalogueServiceResponseToBlobAsync(CloudBlockBlob cloudBlockBlob , SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);            

            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, serializeJsonObject);
                await cloudBlockBlob.UploadFromStreamAsync(ms);
            }
        }

        private SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId,CloudBlockBlob cloudBlockBlob)
        {
            int fileSize = GetFilesize(salesCatalogueResponse);
            var scsResponseQueueMessage = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = batchId,
                ScsResponseUri = cloudBlockBlob.Uri.AbsoluteUri,
                FileSize = fileSize,
                CallbackUri = callBackUri == null ? string.Empty : callBackUri,
                CorrelationId = correlationId
            };
            return scsResponseQueueMessage;
        }

        public CloudBlockBlob GetCloudBlockBlob(string fileName, string storageAccountConnectionString, string containerName)
        {
            CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(containerName);
            CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
            return cloudBlockBlob;
        }

        private void LoadStreamWithJson(Stream ms, object obj)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

        private int GetFilesize(SalesCatalogueProductResponse salesCatalogueResponse)
        {
            int fileSizeCount = 0;
            if(salesCatalogueResponse != null && salesCatalogueResponse.ProductCounts.ReturnedProductCount > 0 )
            {
                foreach (var item in salesCatalogueResponse.Products)
                {
                    fileSizeCount += item.FileSize.Value;
                }                
            }
            return fileSizeCount;
        }
    }
}