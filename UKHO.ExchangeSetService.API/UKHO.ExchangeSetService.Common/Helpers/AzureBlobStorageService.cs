using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Storage;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly ISalesCatalogueStorageService scsStorageService;
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
        
        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse)
        {
            var uploadFileName = string.Concat(batchId, ".json");
            var fileSize = salesCatalogueResponse.Products?.Sum(p => (long)p.FileSize) ?? 0;
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            var instanceCountAndType = GetInstanceCountBasedOnFileSize(fileSizeInMB);
            var (saName,saKey) = GetStorageAccountNameAndKeyBasedOnExchangeSetType(instanceCountAndType.ExchangeSetType);

            var connectionString = scsStorageService.GetStorageAccountConnectionString(saName, saKey);
            //rhz var blobClient = await azureBlobStorageClient.GetBlobClient(uploadFileName, connectionString, containerName);

            //var scsStorageSuccessful = await UploadSalesCatalogueServiceResponseToBlobAsync(blobClient, salesCatalogueResponse);
            //rhz this may return a tuple with the success and the uri
            var scsAbsoluteUri = await UploadSalesCatalogueServiceResponseAsync(uploadFileName, connectionString, containerName, salesCatalogueResponse);
            //if (scsStorageSuccessful)
            if(!string.IsNullOrEmpty(scsAbsoluteUri))
            {
                logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Sales catalogue service response stored to blob storage with fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);

                var scsResponseQueueMessage = new SalesCatalogueServiceResponseQueueMessage()
                {
                    BatchId = batchId,
                    //ScsResponseUri = blobClient.Uri.AbsoluteUri,
                    ScsResponseUri = scsAbsoluteUri,
                    FileSize = fileSize,
                    CallbackUri = callBackUri ?? string.Empty,
                    ExchangeSetStandard = exchangeSetStandard,
                    CorrelationId = correlationId,
                    ExchangeSetUrlExpiryDate = expiryDate,
                    ScsRequestDateTime = scsRequestDateTime,
                    IsEmptyEncExchangeSet = isEmptyEncExchangeSet,
                    IsEmptyAioExchangeSet = isEmptyAioExchangeSet,
                    RequestedProductCount = exchangeSetResponse?.RequestedProductCount ?? 0,
                    RequestedAioProductCount = exchangeSetResponse?.RequestedAioProductCount ?? 0,
                    RequestedProductsAlreadyUpToDateCount = exchangeSetResponse?.RequestedProductsAlreadyUpToDateCount ?? 0,
                    RequestedAioProductsAlreadyUpToDateCount = exchangeSetResponse?.RequestedAioProductsAlreadyUpToDateCount ?? 0
                };

                await AddQueueMessage(scsResponseQueueMessage, instanceCountAndType.InstanceNumber, connectionString); 
            } else
            {
                logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Critical error, Sales catalogue service response not stored to blob storage for  fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);
            }
            //return scsStorageSuccessful;
            return !string.IsNullOrEmpty(scsAbsoluteUri);
        }

        public async Task AddQueueMessage(SalesCatalogueServiceResponseQueueMessage message,int instanceNumber,string storageAccountConnectionString)
        {
            var scsResponseQueueMessageJSON = JsonSerializer.Serialize(message);
            await azureMessageQueueHelper.AddMessage(message.BatchId, instanceNumber, storageAccountConnectionString, scsResponseQueueMessageJSON, message.CorrelationId);
        }

        public async Task<bool> UploadSalesCatalogueServiceResponseToBlobAsync(BlobClient blobClient, SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var uploadSuccess = false;
            var serializeJsonObject = JsonSerializer.Serialize(salesCatalogueResponse);
            
            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, serializeJsonObject);
            try
            {
                await blobClient.UploadAsync(ms);
                uploadSuccess = true;
            }
            catch (Exception ex)
            {
                logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Critical Error, stream upload failed: {message} stream source {sjo} ", ex.Message, serializeJsonObject);
            }
            return uploadSuccess;
        }

        private async Task<string> UploadSalesCatalogueServiceResponseAsync(string fileName, string connectionString, string containerName, SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var absoluteUri = string.Empty;
            var blobClient = await azureBlobStorageClient.GetBlobClient(fileName,connectionString, containerName);
            var serializeJsonObject = JsonSerializer.Serialize(salesCatalogueResponse);

            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, serializeJsonObject);
            try
            {
                // will throw exception if file already exists
                var response = await blobClient.UploadAsync(ms);
                absoluteUri = blobClient.Uri.AbsoluteUri;
            }
            catch (Exception ex)
            {
                logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Critical Error, stream upload failed: {message} stream source {sjo} ", ex.Message, serializeJsonObject);
            }
            return absoluteUri;
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
                async () =>
                {
                    var keyCredential = scsStorageService.GetStorageSharedKeyCredentials();

                    var responseFile = await azureBlobStorageClient.DownloadTextAsync(scsResponseUri, keyCredential);
                    SalesCatalogueProductResponse salesCatalogueProductResponse = JsonSerializer.Deserialize<SalesCatalogueProductResponse>(responseFile);

                    return salesCatalogueProductResponse;
                },
                scsResponseUri, batchId, correlationId);
        }

        private (int InstanceNumber, ExchangeSetType ExchangeSetType) GetInstanceCountBasedOnFileSize(double fileSizeInMB)
        {
            var storageCfg = storageConfig.Value;
            return fileSizeInMB switch
            {
                var fs when fs <= storageCfg.SmallExchangeSetSizeInMB =>
                    (smallExchangeSetInstance.GetInstanceNumber(storageCfg.SmallExchangeSetInstance), ExchangeSetType.sxs),
                var fs when fs > storageCfg.SmallExchangeSetSizeInMB && fs <= storageCfg.LargeExchangeSetSizeInMB =>
                    (mediumExchangeSetInstance.GetInstanceNumber(storageCfg.MediumExchangeSetInstance), ExchangeSetType.mxs),
                var fs when fs > storageCfg.LargeExchangeSetSizeInMB && fs <= storageCfg.LargeMediaExchangeSetSizeInMB =>
                    (largeExchangeSetInstance.GetInstanceNumber(storageCfg.LargeExchangeSetInstance), ExchangeSetType.lxs),
                _ => (largeExchangeSetInstance.GetInstanceNumber(1), ExchangeSetType.lxs),

            };
        }

        public (string Name, string Key) GetStorageAccountNameAndKeyBasedOnExchangeSetType(ExchangeSetType exchangeSetType)
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
