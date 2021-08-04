using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
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
        private readonly ISmallExchangeSetInstance smallExchangeSetInstance;
        private readonly IMediumExchangeSetInstance mediumExchangeSetInstance;
        private readonly ILargeExchangeSetInstance largeExchangeSetInstance;

        public AzureBlobStorageService(ISalesCatalogueStorageService scsStorageService, IOptions<EssFulfilmentStorageConfiguration> storageConfig, 
            IAzureMessageQueueHelper azureMessageQueueHelper, ILogger<AzureBlobStorageService> logger, IAzureBlobStorageClient azureBlobStorageClient,
            ISmallExchangeSetInstance smallExchangeSetInstance, IMediumExchangeSetInstance mediumExchangeSetInstance, 
            ILargeExchangeSetInstance largeExchangeSetInstance)
        {
            this.scsStorageService = scsStorageService;
            this.storageConfig = storageConfig;
            this.azureMessageQueueHelper = azureMessageQueueHelper;
            this.logger = logger;
            this.azureBlobStorageClient = azureBlobStorageClient;
            this.smallExchangeSetInstance = smallExchangeSetInstance;
            this.mediumExchangeSetInstance = mediumExchangeSetInstance;
            this.largeExchangeSetInstance = largeExchangeSetInstance;
        }

        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CancellationToken cancellationToken, string expiryDate)
        {
            string uploadFileName = string.Concat(batchId, ".json");
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            var instanceCountAndType = GetInstanceCountBasedOnFileSize(fileSizeInMB);
            var storageAccountWithKey = GetStorageAccountNameAndKey(instanceCountAndType.Item2);

            string storageAccountConnectionString =
                  scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
            CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            await UploadSalesCatalogueServiceResponseToBlobAsync(cloudBlockBlob, salesCatalogueResponse);

            await AddQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, instanceCountAndType.Item1, storageAccountConnectionString, expiryDate);

            logger.LogInformation(EventIds.SCSResponseStoredAndSentMessageInQueue.ToEventId(), "Sales catalogue response saved for the {batchId}", batchId);
            return true;
        }

        public async Task AddQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob, int instanceNumber, string storageAccountConnectionString, string expiryDate)
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetSalesCatalogueServiceResponseQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, expiryDate);
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(scsResponseQueueMessage);
            await azureMessageQueueHelper.AddMessage(batchId, instanceNumber, storageAccountConnectionString, scsResponseQueueMessageJSON, correlationId);
        }

        public async Task UploadSalesCatalogueServiceResponseToBlobAsync(CloudBlockBlob cloudBlockBlob , SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);            

            using (var ms = new MemoryStream())
            {
                LoadStreamWithJson(ms, serializeJsonObject);
                await azureBlobStorageClient.UploadFromStreamAsync(cloudBlockBlob,ms);
            }
        }

        private SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId,CloudBlockBlob cloudBlockBlob, string expiryDate)
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
        
        public async Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(string scsResponseUri,string correlationId)
        {
            logger.LogInformation(EventIds.DownloadSalesCatalogueResponsDataStart.ToEventId(), "Sales catalogue response download start from blob for the scsResponseUri:{scsResponseUri} and _X-Correlation-ID:{correlationId}", scsResponseUri, correlationId);
            
            string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
            CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlobByUri(scsResponseUri, storageAccountConnectionString);

            var responseFile = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
            SalesCatalogueProductResponse salesCatalogueProductResponse = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(responseFile);
            
            logger.LogInformation(EventIds.DownloadSalesCatalogueResponsDataCompleted.ToEventId(), "Sales catalogue response download completed from blob for the scsResponseUri:{scsResponseUri} and _X-Correlation-ID:{correlationId}", scsResponseUri, correlationId);
            return salesCatalogueProductResponse;
        }


        private (int, string) GetInstanceCountBasedOnFileSize(double fileSizeInMB)
        {
            if (fileSizeInMB <= storageConfig.Value.SmallExchangeSetSizeInMB)
            {
                return (smallExchangeSetInstance.GetInstanceNumber(storageConfig.Value.SmallExchangeSetInstance), ExchangeSetType.SmallExchangeSet.ToString());
            }
            else if (fileSizeInMB > storageConfig.Value.SmallExchangeSetSizeInMB && fileSizeInMB <= storageConfig.Value.LargeExchangeSetSizeInMB)
            {
                return (mediumExchangeSetInstance.GetInstanceNumber(storageConfig.Value.MediumExchangeSetInstance), ExchangeSetType.MediumExchangeSet.ToString());
            }
            else
            {
                return (largeExchangeSetInstance.GetInstanceNumber(storageConfig.Value.LargeExchangeSetInstance), ExchangeSetType.LargeExchangeSet.ToString());
            }
        }

        private (string, string) GetStorageAccountNameAndKey(string exchangeSetType)
        {
            if (string.Compare(exchangeSetType, ExchangeSetType.SmallExchangeSet.ToString(), true) == 0)
            {
                return (storageConfig.Value.SmallExchangeSetAccountName, storageConfig.Value.SmallExchangeSetAccountKey);
            }
            else if (string.Compare(exchangeSetType, ExchangeSetType.MediumExchangeSet.ToString(), true) == 0)
            {
                return (storageConfig.Value.MediumExchangeSetAccountName, storageConfig.Value.MediumExchangeSetAccountKey);
            }
            else
            {
                return (storageConfig.Value.LargeExchangeSetAccountName, storageConfig.Value.LargeExchangeSetAccountKey);
            }
        }
    }
}