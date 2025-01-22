// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue.V2;
using UKHO.ExchangeSetService.Common.Models.V2;
using UKHO.ExchangeSetService.Common.Models.V2.Response;

namespace UKHO.ExchangeSetService.Common.Helpers.V2
{
    public class AzureStorageBlobService : IAzureStorageBlobService
    {
        private readonly ILogger<AzureStorageBlobService> _logger;
        private readonly IAzureBlobStorageClient _azureBlobStorageClient;
        private readonly IAzureMessageQueueHelper _azureMessageQueueHelper;
        private readonly IOptions<EssFulfilmentStorageConfiguration> _essFulfilmentStorageconfig;

        public AzureStorageBlobService(ILogger<AzureStorageBlobService> logger, IAzureBlobStorageClient azureBlobStorageClient, IAzureMessageQueueHelper azureMessageQueueHelper, IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
            _azureBlobStorageClient = azureBlobStorageClient ?? throw new ArgumentNullException(nameof(azureBlobStorageClient));
            _azureMessageQueueHelper = azureMessageQueueHelper ?? throw new ArgumentNullException(nameof(azureMessageQueueHelper));
            _essFulfilmentStorageconfig = essFulfilmentStorageconfig ?? throw new ArgumentNullException(nameof(essFulfilmentStorageconfig));
        }

        /// <summary>
        /// Stores the Sales Catalogue Service response in Azure Blob Storage and adds a message to the queue.
        /// </summary>
        /// <param name="containerName">The name of the Azure Blob Storage container.</param>
        /// <param name="batchId">The batch ID for the current operation.</param>
        /// <param name="salesCatalogueResponse">The response from the Sales Catalogue Service.</param>
        /// <param name="callBackUri">The callback URI for the response.</param>
        /// <param name="exchangeSetStandard">The exchange set standard.</param>
        /// <param name="correlationId">The correlation ID for the current operation.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="expiryDate">The expiry date for the exchange set URL.</param>
        /// <param name="scsRequestDateTime">The request date and time for the Sales Catalogue Service.</param>
        /// <param name="isEmptyExchangeSet">Indicates whether the exchange set is empty.</param>
        /// <param name="exchangeSetResponse">The response for the exchange set standard.</param>
        /// <param name="apiVersion">The API version.</param>
        /// <param name="productIdentifier">The product identifier (optional).</param>
        /// <returns>Returns a boolean indicating whether the storage operation was successful.</returns>
        public async Task<bool> StoreSalesCatalogueServiceResponseAsync(string containerName, string batchId, V2SalesCatalogueProductResponse salesCatalogueResponse, string callBackUri, string exchangeSetStandard, string correlationId, CancellationToken cancellationToken, string expiryDate, DateTime scsRequestDateTime, bool isEmptyExchangeSet, ExchangeSetStandardResponse exchangeSetResponse, ApiVersion apiVersion, string productIdentifier = "")
        {
            var uploadFileName = string.Concat(batchId, ".json");
            var fileSize = salesCatalogueResponse.Products?.Sum(p => (long)p.FileSize) ?? 0;
            var fileSizeInMB = CommonHelper.ConvertBytesToMegabytes(fileSize);
            var connectionString = GetStorageConnectionString(_essFulfilmentStorageconfig.Value.ExchangeSetStorageAccountName, _essFulfilmentStorageconfig.Value.ExchangeSetStorageAccountKey);
            var blobClient = await _azureBlobStorageClient.GetBlobClient(uploadFileName, connectionString, containerName);
            var scsStorageSuccessful = await UploadSalesCatalogueServiceResponseToBlobAsync(blobClient, salesCatalogueResponse);

            if (scsStorageSuccessful)
            {
                _logger.LogInformation(EventIds.ResponseStoredToBlobStorage.ToEventId(), "Response stored to blob storage with fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);

                var scsResponseQueueMessage = new ExchangeSetStandardQueueMessage()
                {
                    BatchId = batchId,
                    ScsResponseUri = blobClient.Uri.AbsoluteUri,
                    FileSize = fileSize,
                    CallbackUri = callBackUri ?? string.Empty,
                    ExchangeSetStandard = exchangeSetStandard,
                    CorrelationId = correlationId,
                    ExchangeSetUrlExpiryDate = expiryDate,
                    ScsRequestDateTime = scsRequestDateTime,
                    IsEmptyExchangeSet = isEmptyExchangeSet,
                    RequestedProductCount = exchangeSetResponse?.RequestedProductCount ?? 0,
                    RequestedProductsAlreadyUpToDateCount = exchangeSetResponse?.RequestedProductsAlreadyUpToDateCount ?? 0,
                    ProductIdentifier = productIdentifier,
                    Version = GetEnumMemberAttrValue(apiVersion),
                };

                await AddQueueMessage(scsResponseQueueMessage, connectionString);
            }
            else
            {
                _logger.LogInformation(EventIds.ResponseFailedStoredToBlobStorage.ToEventId(), "Response not stored to blob storage for  fileSizeInMB:{fileSizeInMB} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId} ", fileSizeInMB, batchId, correlationId);
            }
            return scsStorageSuccessful;
        }

        /// <summary>
        /// Adds a message to the Azure message queue.
        /// </summary>
        /// <param name="message">The message to be added to the queue.</param>
        /// <param name="storageAccountConnectionString">The connection string for the Azure storage account.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task AddQueueMessage(ExchangeSetStandardQueueMessage message, string storageAccountConnectionString)
        {
            var scsResponseQueueMessageJSON = JsonConvert.SerializeObject(message);
            await _azureMessageQueueHelper.AddMessage(message.BatchId, storageAccountConnectionString, scsResponseQueueMessageJSON, message.CorrelationId);
        }

        /// <summary>
        /// Uploads the Sales Catalogue Service response to Azure Blob Storage.
        /// </summary>
        /// <param name="blobClient">The BlobClient instance used to interact with the blob storage.</param>
        /// <param name="salesCatalogueResponse">The response from the Sales Catalogue Service.</param>
        /// <returns>Returns a boolean indicating whether the upload operation was successful.</returns>
        public async Task<bool> UploadSalesCatalogueServiceResponseToBlobAsync(BlobClient blobClient, V2SalesCatalogueProductResponse salesCatalogueResponse)
        {
            var uploadSuccess = false;
            var serializeJsonObject = JsonConvert.SerializeObject(salesCatalogueResponse);

            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, serializeJsonObject);
            try
            {
                await blobClient.UploadAsync(ms);
                uploadSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(EventIds.StreamUploadFailed.ToEventId(), "Critical Error, stream upload failed: {message} stream source {sjo} ", ex.Message, serializeJsonObject);
            }
            return uploadSuccess;
        }

        /// <summary>
        /// Loads a stream with a JSON representation of the provided object.
        /// </summary>
        /// <param name="ms">The stream to load with JSON data.</param>
        /// <param name="obj">The object to serialize to JSON and load into the stream.</param>
        private void LoadStreamWithJson(Stream ms, object obj)
        {
            var writer = new StreamWriter(ms);
            writer.Write(obj);
            writer.Flush();
            ms.Position = 0;
        }

        /// <summary>
        /// Gets the value of the EnumMember attribute for a given enum value.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="enumVal">The enum value.</param>
        /// <returns>The value of the EnumMember attribute if it exists; otherwise, null.</returns>
        private string GetEnumMemberAttrValue<T>(T enumVal)
        {
            var enumType = typeof(T);
            var memInfo = enumType.GetMember(enumVal.ToString());
            var attr = memInfo.FirstOrDefault()?.GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
            if (attr != null)
            {
                return attr.Value;
            }

            return null;
        }

        /// <summary>
        /// Gets the storage connection string for the specified account name and key.
        /// </summary>
        /// <param name="accountName">The name of the storage account.</param>
        /// <param name="accountKey">The key for the storage account.</param>
        /// <returns>The storage connection string.</returns>
        private string GetStorageConnectionString(string accountName, string accountKey)
        {
            return $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";
        }

    }
}
