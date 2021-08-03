using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
        private const string CONTENT_TYPE = "application/json";
        private readonly IOptions<EssFulfilmentStorageConfiguration> storageConfig;
        private readonly IAzureMessageQueueHelper azureMessageQueueHelper;
        private readonly ILogger<AzureBlobStorageService> logger;
        private readonly IAzureBlobStorageClient azureBlobStorageClient;

        public AzureBlobStorageService(ISalesCatalogueStorageService scsStorageService, IOptions<EssFulfilmentStorageConfiguration> storageConfig, IAzureMessageQueueHelper azureMessageQueueHelper, ILogger<AzureBlobStorageService> logger, IAzureBlobStorageClient azureBlobStorageClient)
        {
            this.scsStorageService = scsStorageService;
            this.storageConfig = storageConfig;
            this.azureMessageQueueHelper = azureMessageQueueHelper;
            this.logger = logger;
            this.azureBlobStorageClient = azureBlobStorageClient;
        }

        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CancellationToken cancellationToken, string expiryDate)
        {
            string uploadFileName = string.Concat(batchId, ".json");

            string storageAccountConnectionString =
                  scsStorageService.GetStorageAccountConnectionString();
            CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            await UploadSalesCatalogueServiceResponseToBlobAsync(cloudBlockBlob, salesCatalogueResponse);

            await AddQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, expiryDate);

            logger.LogInformation(EventIds.SCSResponseStoredAndSentMessageInQueue.ToEventId(), "Sales catalogue response saved for the {batchId} and _X-Correlation-ID:{CorrelationId} ", batchId, correlationId);
            return true;
        }

        public async Task AddQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob, string expiryDate)
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetSalesCatalogueServiceResponseQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, expiryDate);
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(scsResponseQueueMessage);
            await azureMessageQueueHelper.AddMessage(storageConfig.Value, scsResponseQueueMessageJSON);
        }

        public async Task UploadSalesCatalogueServiceResponseToBlobAsync(CloudBlockBlob cloudBlockBlob, SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);

            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, serializeJsonObject);
                await azureBlobStorageClient.UploadFromStreamAsync(cloudBlockBlob, ms);
            }
        }

        private SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob, string expiryDate)
        {
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var scsResponseQueueMessage = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = batchId,
                ScsResponseUri = cloudBlockBlob.Uri.AbsoluteUri,
                FileSize = fileSize,
                CallbackUri = callBackUri == null ? string.Empty : callBackUri,
                CorrelationId = correlationId,
                ExchangeSetUrlExpiryDate = expiryDate
            };
            return scsResponseQueueMessage;
        }

        private void LoadStreamWithJson(Stream ms, object obj)
        {
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(string scsResponseUri, string batchId, string correlationId)
        {
            logger.LogInformation(EventIds.DownloadSalesCatalogueResponseDataStart.ToEventId(), "Sales catalogue response download started from blob for the scsResponseUri:{scsResponseUri} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", scsResponseUri, batchId, correlationId);

            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
            CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlobByUri(scsResponseUri, storageAccountConnectionString);

            var responseFile = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
            SalesCatalogueProductResponse salesCatalogueProductResponse = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(responseFile);
            logger.LogInformation(EventIds.DownloadSalesCatalogueResponseDataCompleted.ToEventId(), "Sales catalogue response download completed from blob for the scsResponseUri:{scsResponseUri} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", scsResponseUri, batchId, correlationId);
            return salesCatalogueProductResponse;
        }
    }
}