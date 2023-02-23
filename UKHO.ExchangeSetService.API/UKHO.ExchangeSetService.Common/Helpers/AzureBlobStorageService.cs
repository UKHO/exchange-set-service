using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
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

        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime)
        {
            string uploadFileName = string.Concat(batchId, ".json");
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            var instanceCountAndType = GetInstanceCountBasedOnFileSize(fileSizeInMB);
            var storageAccountWithKey = GetStorageAccountNameAndKeyBasedOnExchangeSetType(instanceCountAndType.Item2);

            string storageAccountConnectionString =
                  scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
            CloudBlockBlob cloudBlockBlob = await azureBlobStorageClient.GetCloudBlockBlob(uploadFileName, storageAccountConnectionString, containerName);
            cloudBlockBlob.Properties.ContentType = CONTENT_TYPE;

            await UploadSalesCatalogueServiceResponseToBlobAsync(cloudBlockBlob, salesCatalogueResponse);
            logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Sales catalogue service response stored to blob storage with fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);

            await AddQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, instanceCountAndType.Item1, storageAccountConnectionString, expiryDate, scsRequestDateTime);

            return true;
        }

        public async Task AddQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob, int instanceNumber, string storageAccountConnectionString, string expiryDate, DateTime scsRequestDateTime)
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetSalesCatalogueServiceResponseQueueMessage(batchId, salesCatalogueResponse, callBackUri, correlationId, cloudBlockBlob, expiryDate, scsRequestDateTime);
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(scsResponseQueueMessage);
            await azureMessageQueueHelper.AddMessage(batchId, instanceNumber, storageAccountConnectionString, scsResponseQueueMessageJSON, correlationId);
        }

        public async Task UploadSalesCatalogueServiceResponseToBlobAsync(CloudBlockBlob cloudBlockBlob, SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);

            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, serializeJsonObject);
            await azureBlobStorageClient.UploadFromStreamAsync(cloudBlockBlob, ms);

        }

        private SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string correlationId, CloudBlockBlob cloudBlockBlob, string expiryDate, DateTime scsRequestDateTime)
        {
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var scsResponseQueueMessage = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = batchId,
                ScsResponseUri = cloudBlockBlob.Uri.AbsoluteUri,
                FileSize = fileSize,
                CallbackUri = callBackUri ?? string.Empty,
                CorrelationId = correlationId,
                ExchangeSetUrlExpiryDate = expiryDate,
                ScsRequestDateTime = scsRequestDateTime
            };
            return scsResponseQueueMessage;
        }

        private void LoadStreamWithJson(Stream ms, object obj)
        {
            var writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

        public Task<SalesCatalogueProductResponse> DownloadSalesCatalogueResponse(string scsResponseUri, string batchId, string correlationId)
        {
            return logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadSalesCatalogueResponseDataStart,
                EventIds.DownloadSalesCatalogueResponseDataCompleted,
                "Sales catalogue response download from blob for scsResponseUri:{scsResponseUri} and BatchId:{batchId} and _X-Correlation-ID:{correlationId}", 
                async ()=> {
                    string storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString();
                    CloudBlockBlob cloudBlockBlob = azureBlobStorageClient.GetCloudBlockBlobByUri(scsResponseUri, storageAccountConnectionString);

                    var responseFile = await azureBlobStorageClient.DownloadTextAsync(cloudBlockBlob);
                    SalesCatalogueProductResponse salesCatalogueProductResponse = JsonConvert.DeserializeObject<SalesCatalogueProductResponse>(responseFile);
                  
                    return salesCatalogueProductResponse;
                },
                scsResponseUri, batchId, correlationId);
        }


        private (int, ExchangeSetType) GetInstanceCountBasedOnFileSize(double fileSizeInMB)
        {
            if (fileSizeInMB <= storageConfig.Value.SmallExchangeSetSizeInMB)
            {
                return (smallExchangeSetInstance.GetInstanceNumber(storageConfig.Value.SmallExchangeSetInstance), ExchangeSetType.sxs);
            }
            else if (fileSizeInMB > storageConfig.Value.SmallExchangeSetSizeInMB && fileSizeInMB <= storageConfig.Value.LargeExchangeSetSizeInMB)
            {
                return (mediumExchangeSetInstance.GetInstanceNumber(storageConfig.Value.MediumExchangeSetInstance), ExchangeSetType.mxs);
            }
            else if (fileSizeInMB > storageConfig.Value.LargeExchangeSetSizeInMB && fileSizeInMB <= storageConfig.Value.LargeMediaExchangeSetSizeInMB)
            {
                return (largeExchangeSetInstance.GetInstanceNumber(storageConfig.Value.LargeExchangeSetInstance), ExchangeSetType.lxs);
            }
            else
            {
                return (largeExchangeSetInstance.GetInstanceNumber(1), ExchangeSetType.lxs);
            }
        }

        public (string, string) GetStorageAccountNameAndKeyBasedOnExchangeSetType(ExchangeSetType exchangeSetType)
        {
            return exchangeSetType switch
            {
                ExchangeSetType.sxs => (storageConfig.Value.SmallExchangeSetAccountName, storageConfig.Value.SmallExchangeSetAccountKey),
                ExchangeSetType.mxs => (storageConfig.Value.MediumExchangeSetAccountName, storageConfig.Value.MediumExchangeSetAccountKey),
                ExchangeSetType.lxs => (storageConfig.Value.LargeExchangeSetAccountName, storageConfig.Value.LargeExchangeSetAccountKey),
                _ => (string.Empty, string.Empty),
            };
        }

        public int GetInstanceCountBasedOnExchangeSetType(ExchangeSetType exchangeSetType)
        {
            return exchangeSetType switch
            {
                ExchangeSetType.sxs => storageConfig.Value.SmallExchangeSetInstance,
                ExchangeSetType.mxs => storageConfig.Value.MediumExchangeSetInstance,
                ExchangeSetType.lxs => storageConfig.Value.LargeExchangeSetInstance,
                _ => 1,
            };
        }
    }
}