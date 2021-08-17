using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
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

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper,
                                    IFileShareService fileShareService, IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                    IAzureBlobStorageService azureBlobStorageService, IFulfilmentCallBackService fulfilmentCallBackService)
        {
            this.configuration = configuration;
            this.fulFilmentDataService = fulFilmentDataService;
            this.logger = logger;
            this.fileSystemHelper = fileSystemHelper;
            this.fileShareService = fileShareService;
            this.fileShareServiceConfig = fileShareServiceConfig;
            this.azureBlobStorageService = azureBlobStorageService;
            this.fulfilmentCallBackService = fulfilmentCallBackService;
        }

        public async Task ProcessQueueMessage([QueueTrigger("%ESSFulfilmentStorageConfiguration:QueueName%")] CloudQueueMessage message)
        {
            SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage = JsonConvert.DeserializeObject<SalesCatalogueServiceResponseQueueMessage>(message.AsString);
            string homeDirectoryPath = configuration["HOME"];
            string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
            string batchFolderPath = Path.Combine(homeDirectoryPath, currentUtcDate, fulfilmentServiceQueueMessage.BatchId);

            try
            {
                logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set web job started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

                await fulFilmentDataService.CreateExchangeSet(fulfilmentServiceQueueMessage, currentUtcDate);

                logger.LogInformation(EventIds.CreateExchangeSetRequestCompleted.ToEventId(), "Create Exchange Set web job completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
            }
            catch (Exception ex)
            {
                EventId exceptionEventId = EventIds.SystemException.ToEventId();

                if (ex.GetType() == typeof(FulfilmentException))
                    exceptionEventId = ((FulfilmentException)ex).EventId;

                FulfilmentException fulfilmentException = new FulfilmentException(exceptionEventId);
                string errorMessage = string.Format(fulfilmentException.Message, exceptionEventId.Id, fulfilmentServiceQueueMessage.CorrelationId);

                await CreateAndUploadErrorFileToFileShareService(fulfilmentServiceQueueMessage, exceptionEventId, errorMessage, batchFolderPath);

                if (ex.GetType() != typeof(FulfilmentException))
                    logger.LogError(exceptionEventId, ex, "Unhandled exception while processing Exchange Set web job for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId, ex.Message);
            }
        }

        public async Task CreateAndUploadErrorFileToFileShareService(SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage, EventId eventId, string errorMessage, string batchFolderPath)
        {
            fileSystemHelper.CheckAndCreateFolder(batchFolderPath);

            var errorFileFullPath = Path.Combine(batchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
            fileSystemHelper.CreateFileContent(errorFileFullPath, errorMessage);

            if (fileSystemHelper.CheckFileExists(errorFileFullPath))
            {
                var isUploaded = await fileShareService.UploadFileToFileShareService(fulfilmentServiceQueueMessage.BatchId, batchFolderPath, fulfilmentServiceQueueMessage.CorrelationId, fileShareServiceConfig.Value.ErrorFileName);

                if (isUploaded)
                {
                    logger.LogError(EventIds.ErrorTxtIsUploaded.ToEventId(), "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", eventId.Id, eventId.Name, fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
                    logger.LogError(EventIds.ExchangeSetNotCreated.ToEventId(), "Exchange set is not created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
                }
                else
                    logger.LogError(EventIds.ErrorTxtNotUploaded.ToEventId(), "Error while uploading error.txt file to file share service for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
            }
            else
            { 
                logger.LogError(EventIds.ErrorTxtNotCreated.ToEventId(), "Error while creating error.txt for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId); 
            }

            await SendErrorCallBackResponse(fulfilmentServiceQueueMessage);
        }

        public async Task SendErrorCallBackResponse(SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage)
        {
            SalesCatalogueProductResponse salesCatalogueProductResponse = await azureBlobStorageService.DownloadSalesCatalogueResponse(fulfilmentServiceQueueMessage.ScsResponseUri, fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

            await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, fulfilmentServiceQueueMessage);
        }
    }
}
