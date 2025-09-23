using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Elastic.Apm;
using Elastic.Apm.Api;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Extensions;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.Common.Models.WebJobs;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentServiceJob(IConfiguration configuration,
                                IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper,
                                IFileShareBatchService fileBatchShareService, IFileShareUploadService fileShareUploadService, IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                IAzureBlobStorageService azureBlobStorageService, IFulfilmentCallBackService fulfilmentCallBackService,
                                IOptions<PeriodicOutputServiceConfiguration> periodicOutputServiceConfiguration)
    {
        protected IConfiguration configuration = configuration;

        public async Task ProcessQueueMessage([QueueTrigger("%ESSFulfilmentStorageConfiguration:QueueName%")] QueueMessage message)
        {
            //SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage = message.Body.ToObjectFromJson<SalesCatalogueServiceResponseQueueMessage>();
            //string homeDirectoryPath = configuration["HOME"];
            //string currentUtcDate = DateTime.UtcNow.ToString("ddMMMyyyy");
            //string batchFolderPath = Path.Combine(homeDirectoryPath, currentUtcDate, fulfilmentServiceQueueMessage.BatchId);
            //double fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(fulfilmentServiceQueueMessage.FileSize);
            var batch = new FulfilmentServiceBatch(
                configuration["TMP"],
                message.Body.ToObjectFromJson<SalesCatalogueServiceResponseQueueMessage>(),
                fileShareServiceConfig.Value,
                periodicOutputServiceConfiguration.Value
                );
            var fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(batch.Message.FileSize);

            CommonHelper.IsPeriodicOutputService = fileSizeInMb > periodicOutputServiceConfiguration.Value.LargeMediaExchangeSetSizeInMB;

            try
            {
                logger.LogInformation(EventIds.AIOToggleIsOn.ToEventId(), "ESS Webjob : AIO toggle is ON for BatchId:{BatchId} | _X-Correlation-ID : {CorrelationId}", batch.BatchId, batch.CorrelationId);

                string transactionName =
                    $"{Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME")}-fulfilment-transaction";

                await Agent.Tracer.CaptureTransaction(transactionName, ApiConstants.TypeRequest, async () =>
                {
                    if (CommonHelper.IsPeriodicOutputService)
                    {
                        await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateLargeExchangeSetRequestStart,
                            EventIds.CreateLargeExchangeSetRequestCompleted,
                            "Create Large Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                            async () =>
                            {
                                return await fulFilmentDataService.CreateLargeExchangeSet(fulfilmentServiceQueueMessage, currentUtcDate, periodicOutputServiceConfiguration.Value.LargeExchangeSetFolderName);
                            },
                            batch.BatchId, batch.CorrelationId);
                    }
                    else
                    {
                        await logger.LogStartEndAndElapsedTimeAsync(EventIds.CreateExchangeSetRequestStart,
                            EventIds.CreateExchangeSetRequestCompleted,
                            "Create Exchange Set web job request for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                            async () =>
                            {
                                return await fulFilmentDataService.CreateExchangeSet(fulfilmentServiceQueueMessage, currentUtcDate);
                            },
                            batch.BatchId, batch.CorrelationId);
                    }
                });
            }
            catch (Exception ex)
            {
                EventId exceptionEventId = EventIds.SystemException.ToEventId();

                if (ex.GetType() == typeof(FulfilmentException))
                    exceptionEventId = ((FulfilmentException)ex).EventId;

                var fulfilmentException = new FulfilmentException(exceptionEventId);
                var errorMessage = string.Format(fulfilmentException.Message, exceptionEventId.Id, batch.CorrelationId);

                await CreateAndUploadErrorFileToFileShareService(batch, exceptionEventId, errorMessage);

                if (ex.GetType() != typeof(FulfilmentException))
                    logger.LogError(exceptionEventId, ex, "Unhandled exception while processing Exchange Set web job for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", batch.BatchId, batch.CorrelationId, ex.Message);

                Agent.Tracer.CurrentTransaction?.CaptureException(ex);
            }
            finally
            {
                Agent.Tracer.CurrentTransaction?.End();
            }
        }

        public async Task CreateAndUploadErrorFileToFileShareService(FulfilmentServiceBatch batch, EventId eventId, string errorMessage)
        {
            fileSystemHelper.CheckAndCreateFolder(batch.BatchDirectory);

            //var errorFileFullPath = Path.Combine(batch.BatchDirectory, fileShareServiceConfig.Value.ErrorFileName);
            fileSystemHelper.CreateFileContent(batch.ErrorFile, errorMessage);

            if (fileSystemHelper.CheckFileExists(batch.ErrorFile))
            {
                var isErrorFileCommitted = false;
                var isUploaded = await fileShareUploadService.UploadFileToFileShareService(batch.BatchId, batch.BatchDirectory, batch.CorrelationId, fileShareServiceConfig.Value.ErrorFileName);

                if (isUploaded)
                {
                    isErrorFileCommitted = await fileBatchShareService.CommitBatchToFss(batch.BatchId, batch.CorrelationId, batch.BatchDirectory, fileShareServiceConfig.Value.ErrorFileName);
                }

                if (isErrorFileCommitted)
                {
                    logger.LogError(EventIds.ErrorTxtIsUploaded.ToEventId(), "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", eventId.Id, eventId.Name, batch.BatchId, batch.CorrelationId);
                    logger.LogError(EventIds.ExchangeSetCreatedWithError.ToEventId(), "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batch.BatchId, batch.CorrelationId);
                }
                else
                    logger.LogError(EventIds.ErrorTxtNotUploaded.ToEventId(), "Error while uploading error.txt file to file share service for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batch.BatchId, batch.CorrelationId);
            }
            else
            {
                logger.LogError(EventIds.ErrorTxtNotCreated.ToEventId(), "Error while creating error.txt for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", batch.BatchId, batch.CorrelationId);
            }

            await SendErrorCallBackResponse(batch);
        }

        public async Task SendErrorCallBackResponse(FulfilmentServiceBatch batch)
        {
            var salesCatalogueProductResponse = await azureBlobStorageService.DownloadSalesCatalogueResponse(batch.Message.ScsResponseUri, batch.BatchId, batch.CorrelationId);

            await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, batch.Message);
        }
    }
}
