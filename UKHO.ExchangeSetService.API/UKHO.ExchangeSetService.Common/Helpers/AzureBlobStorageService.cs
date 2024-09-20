using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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

        public async Task<bool> StoreSaleCatalogueServiceResponseAsync(string containerName, string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse)
        {
            string uploadFileName = string.Concat(batchId, ".json");
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            var instanceCountAndType = GetInstanceCountBasedOnFileSize(fileSizeInMB);
            var storageAccountWithKey = GetStorageAccountNameAndKeyBasedOnExchangeSetType(instanceCountAndType.Item2);

            var storageAccountConnectionString = scsStorageService.GetStorageAccountConnectionString(storageAccountWithKey.Item1, storageAccountWithKey.Item2);
            logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "Diagnostic GetBlobClient Data FileName:{uploadFileName}, ContainerName:{containerName} ", uploadFileName, containerName);
            // rhz the following returns a blob client with uri, 
            var blobClient = await azureBlobStorageClient.GetBlobClient(uploadFileName, storageAccountConnectionString, containerName);
            
            logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "Diagnostic get BlobClient URI:{blobClient.Uri}", blobClient.Uri); //rhz

            try
            {
                await blobClient?.SetHttpHeadersAsync(new BlobHttpHeaders { ContentType = CONTENT_TYPE }, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "Diagnostic set HttpHeaders failed :{errorMessage}", ex.Message); //rhz
            }

            await UploadSalesCatalogueServiceResponseToBlobAsync(blobClient, salesCatalogueResponse);
            logger.LogInformation(EventIds.SCSResponseStoredToBlobStorage.ToEventId(), "Sales catalogue service response stored to blob storage with fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);

            await AddQueueMessage(batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, blobClient, instanceCountAndType.Item1, storageAccountConnectionString, expiryDate, scsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetResponse);

            return true;
        }

        public async Task AddQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, BlobClient blobClient, int instanceNumber, string storageAccountConnectionString, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse)
        {
            SalesCatalogueServiceResponseQueueMessage scsResponseQueueMessage = GetSalesCatalogueServiceResponseQueueMessage(batchId, salesCatalogueResponse, callBackUri, exchangeSetStandard, correlationId, blobClient, expiryDate, scsRequestDateTime, isEmptyEncExchangeSet, isEmptyAioExchangeSet, exchangeSetResponse);
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(scsResponseQueueMessage);
            await azureMessageQueueHelper.AddMessage(batchId, instanceNumber, storageAccountConnectionString, scsResponseQueueMessageJSON, correlationId);
        }

        public async Task UploadSalesCatalogueServiceResponseToBlobAsync(BlobClient blobClient, SalesCatalogueProductResponse salesCatalogueResponse)
        {
            logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "Diagnostic Upload to blob"); //rhz
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);
            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, serializeJsonObject);
            ////await azureBlobStorageClient.UploadFromStreamAsync(blobClient, ms);
            try
            {
                await blobClient.UploadAsync(ms);
            }
            catch (Exception ex)
            {
                logger.LogInformation(EventIds.SCSResponseStoreRequestStart.ToEventId(), "Diagnostic stream upload failed: {message} stream source {sjo} ",ex.Message, serializeJsonObject);
            }
            
        }

        private SalesCatalogueServiceResponseQueueMessage GetSalesCatalogueServiceResponseQueueMessage(string batchId, SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, BlobClient blobClient, string expiryDate, DateTime scsRequestDateTime, bool isEmptyEncExchangeSet, bool isEmptyAioExchangeSet, ExchangeSetResponse exchangeSetResponse)
        {
            long fileSize = CommonHelper.GetFileSize(salesCatalogueResponse);
            var scsResponseQueueMessage = new SalesCatalogueServiceResponseQueueMessage()
            {
                BatchId = batchId,
                ScsResponseUri = blobClient.Uri.AbsoluteUri,
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
                async () =>
                {
                    var keyCredential = scsStorageService.GetStorageSharedKeyCredentials();
                    var blobClient = azureBlobStorageClient.GetBlobClientByUri(scsResponseUri, keyCredential);

                    var responseFile = await azureBlobStorageClient.DownloadTextAsync(blobClient);
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