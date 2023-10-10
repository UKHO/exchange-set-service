using Azure.Storage.Queues.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentServiceJob
    {
        protected IConfiguration configuration;
        private readonly IFulfilmentDataService fulFilmentDataService;
        private readonly ILogger<FulfilmentServiceJob> logger;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly IFileShareService fileShareService;
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;
        private readonly IAzureBlobStorageService azureBlobStorageService;
        private readonly IFulfilmentCallBackService fulfilmentCallBackService;
        private readonly IOptions<PeriodicOutputServiceConfiguration> periodicOutputServiceConfiguration;
        private readonly IOptions<AioConfiguration> aioConfiguration;

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper,
                                    IFileShareService fileShareService, IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IAzureBlobStorageService azureBlobStorageService, IFulfilmentCallBackService fulfilmentCallBackService,
                                    IOptions<PeriodicOutputServiceConfiguration> periodicOutputServiceConfiguration, IOptions<AioConfiguration> aioConfiguration)
        {
            this.configuration = configuration;
            this.fulFilmentDataService = fulFilmentDataService;
            this.logger = logger;
            this.fileSystemHelper = fileSystemHelper;
            this.fileShareService = fileShareService;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentCallBackService = fulfilmentCallBackService;
            this.periodicOutputServiceConfiguration = periodicOutputServiceConfiguration;
            this.aioConfiguration = aioConfiguration;
        }

        public async Task ProcessQueueMessage([QueueTrigger("%ESSFulfilmentStorageConfiguration:QueueName%")] QueueMessage message)
        {
            SalesCatalogueServiceResponseQueueMessage fulfillmentServiceQueueMessage =
                message.Body.ToObjectFromJson<SalesCatalogueServiceResponseQueueMessage>();
            string homeDirectoryPath = configuration["HOME"];
            string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
            string batchFolderPath = Path.Combine(homeDirectoryPath, currentUtcDate,
                fulfillmentServiceQueueMessage.BatchId);
            double fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(fulfillmentServiceQueueMessage.FileSize);

            CommonHelper.IsPeriodicOutputService =
                fileSizeInMb > periodicOutputServiceConfiguration.Value.LargeMediaExchangeSetSizeInMB;
            try
            {
                try
                {
                    if (aioConfiguration.Value.IsAioEnabled)
                    {
                        logger.LogInformation(EventIds.AIOToggleIsOn.ToEventId(),
                            "ESS Webjob : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}",
                            fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                    }
                    else
                    {
                        logger.LogInformation(EventIds.AIOToggleIsOff.ToEventId(),
                            "ESS Webjob : AIO toggle is OFF for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}",
                            fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                    }

                    if (CommonHelper.IsPeriodicOutputService)
                    {
                        await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateLargeExchangeSetRequestStart,
                            EventIds.CreateLargeExchangeSetRequestCompleted,
                            "Create Large Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                            async () =>
                            {
                                return await fulFilmentDataService.CreateLargeExchangeSet(fulfillmentServiceQueueMessage,
                                    currentUtcDate,
                                    periodicOutputServiceConfiguration.Value.LargeExchangeSetFolderName);
                            },
                            fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                    }
                    else
                    {
                        await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateExchangeSetRequestStart,
                            EventIds.CreateExchangeSetRequestCompleted,
                            "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                            async () =>
                            {
                                return await fulFilmentDataService.CreateExchangeSet(fulfillmentServiceQueueMessage,
                                    currentUtcDate);
                            },
                            fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                    }
                }
                catch (FulfilmentException ex)
                {
                    EventId exceptionEventId = ex.EventId;
                    var fulfillmentException = new FulfilmentException(exceptionEventId);
                    string errorMessage = string.Format(fulfillmentException.Message, exceptionEventId.Id,
                        fulfillmentServiceQueueMessage.CorrelationId);

                    await CreateAndUploadErrorFileToFileShareService(fulfillmentServiceQueueMessage, exceptionEventId,
                        errorMessage, batchFolderPath);
                }
            }
            catch (Exception ex)
            {
                EventId exceptionEventId = EventIds.SystemException.ToEventId();

                logger.LogError(exceptionEventId, ex,
                    "Unhandled exception while processing Exchange Set web job for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}",
                    fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId,
                    ex.Message);
            }
        }

        public async Task CreateAndUploadErrorFileToFileShareService(SalesCatalogueServiceResponseQueueMessage fulfillmentServiceQueueMessage, EventId eventId, string errorMessage, string batchFolderPath)
        {
            fileSystemHelper.CheckAndCreateFolder(batchFolderPath);

            var errorFileFullPath = Path.Combine(batchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
            fileSystemHelper.CreateFileContent(errorFileFullPath, errorMessage);

            if (fileSystemHelper.CheckFileExists(errorFileFullPath))
            {
                var isErrorFileCommitted = false;
                var isUploaded = await fileShareService.UploadFileToFileShareService(fulfillmentServiceQueueMessage.BatchId, batchFolderPath, fulfillmentServiceQueueMessage.CorrelationId, fileShareServiceConfig.Value.ErrorFileName);

                if (isUploaded)
                {
                    isErrorFileCommitted = await fileShareService.CommitBatchToFss(fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId, batchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
                }

                if (isErrorFileCommitted)
                {
                    logger.LogError(EventIds.ErrorTxtIsUploaded.ToEventId(), "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", eventId.Id, eventId.Name, fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                    logger.LogError(EventIds.ExchangeSetCreatedWithError.ToEventId(), "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
                }
                else
                    logger.LogError(EventIds.ErrorTxtNotUploaded.ToEventId(), "Error while uploading error.txt file to file share service for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
            }
            else
            {
                logger.LogError(EventIds.ErrorTxtNotCreated.ToEventId(), "Error while creating error.txt for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfillmentServiceQueueMessage.BatchId, fulfillmentServiceQueueMessage.CorrelationId);
            }

            await SendErrorCallBackResponse(fulfillmentServiceQueueMessage);
        }

        public async Task SendErrorCallBackResponse(SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage)
        {
            SalesCatalogueProductResponse salesCatalogueProductResponse = await azureBlobStorageService.DownloadSalesCatalogueResponse(fulfilmentServiceQueueMessage.ScsResponseUri, fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

            await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, fulfilmentServiceQueueMessage);
        }
    }
}
