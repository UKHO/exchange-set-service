using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentServiceJob(IConfiguration configuration,
                                IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper,
                                IFileShareBatchService fileBatchShareService, IFileShareUploadService fileShareUploadService, IOptions<FileShareServiceConfiguration> fileShareServiceConfig,
                                IAzureBlobStorageService azureBlobStorageService, IFulfilmentCallBackService fulfilmentCallBackService,
                                IOptions<PeriodicOutputServiceConfiguration> periodicOutputServiceConfiguration, IFulfilmentCleanUpService fulfilmentCleanUpService, IOptions<CleanUpConfiguration> cleanUpConfiguration)
    {
        private readonly CleanUpConfiguration _cleanUpConfiguration = cleanUpConfiguration?.Value ?? throw new ArgumentNullException(nameof(cleanUpConfiguration));

        public async Task ProcessQueueMessage([QueueTrigger("%ESSFulfilmentStorageConfiguration:QueueName%")] QueueMessage message)
        {
            var salesCatalogueServiceResponseQueueMessage = message.Body.ToObjectFromJson<SalesCatalogueServiceResponseQueueMessage>();
            var batch = new FulfilmentServiceBatch(configuration, salesCatalogueServiceResponseQueueMessage, DateTime.UtcNow);
            var fileSizeInMb = CommonHelper.ConvertBytesToMegabytes(batch.Message.FileSize);
            CommonHelper.IsPeriodicOutputService = fileSizeInMb > periodicOutputServiceConfiguration.Value.LargeMediaExchangeSetSizeInMB;

            try
            {
                var transactionName =
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
                                return await fulFilmentDataService.CreateLargeExchangeSet(batch, periodicOutputServiceConfiguration.Value.LargeExchangeSetFolderName);
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
                                return await fulFilmentDataService.CreateExchangeSet(batch);
                            },
                            batch.BatchId, batch.CorrelationId);
                    }
                });
            }
            catch (Exception ex)
            {
                var exceptionEventId = EventIds.SystemException.ToEventId();

                if (ex.GetType() == typeof(FulfilmentException))
                    exceptionEventId = ((FulfilmentException)ex).EventId;

                var fulfilmentException = new FulfilmentException(exceptionEventId);
                var errorMessage = string.Format(fulfilmentException.Message, exceptionEventId.Id, batch.CorrelationId);

                await CreateAndUploadErrorFileToFileShareService(batch.Message, exceptionEventId, errorMessage, batch.BatchDirectory);

                if (ex.GetType() != typeof(FulfilmentException))
                    logger.LogError(exceptionEventId, ex, "Unhandled exception while processing Exchange Set web job for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId} and Exception:{Message}", batch.BatchId, batch.CorrelationId, ex.Message);

                Agent.Tracer.CurrentTransaction?.CaptureException(ex);
            }
            finally
            {
                CleanUpTemporaryBatchData(batch);
            }
        }

        public async Task CreateAndUploadErrorFileToFileShareService(SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage, EventId eventId, string errorMessage, string batchFolderPath)
        {
            fileSystemHelper.CheckAndCreateFolder(batchFolderPath);

            var errorFileFullPath = Path.Combine(batchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
            fileSystemHelper.CreateFileContent(errorFileFullPath, errorMessage);

            if (fileSystemHelper.CheckFileExists(errorFileFullPath))
            {
                var isErrorFileCommitted = false;
                var isUploaded = await fileShareUploadService.UploadFileToFileShareService(fulfilmentServiceQueueMessage.BatchId, batchFolderPath, fulfilmentServiceQueueMessage.CorrelationId, fileShareServiceConfig.Value.ErrorFileName);

                if (isUploaded)
                {
                    isErrorFileCommitted = await fileBatchShareService.CommitBatchToFss(fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId, batchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
                }

                if (isErrorFileCommitted)
                {
                    logger.LogError(EventIds.ErrorTxtIsUploaded.ToEventId(), "Error while processing Exchange Set creation and error.txt file is created and uploaded in file share service with ErrorCode-EventId:{EventId} and EventName:{EventName} for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", eventId.Id, eventId.Name, fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
                    logger.LogError(EventIds.ExchangeSetCreatedWithError.ToEventId(), "Exchange set is created with error for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
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
            var salesCatalogueProductResponse = await azureBlobStorageService.DownloadSalesCatalogueResponse(fulfilmentServiceQueueMessage.ScsResponseUri, fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

            await fulfilmentCallBackService.SendCallBackErrorResponse(salesCatalogueProductResponse, fulfilmentServiceQueueMessage);
        }

        private void CleanUpTemporaryBatchData(FulfilmentServiceBatch batch)
        {
            try
            {
                if (_cleanUpConfiguration.ContinuousCleanupEnabled)
                {
                    logger.LogStartEndAndElapsedTime(EventIds.FulfilmentBatchCleanUpStarted, EventIds.FulfilmentBatchCleanUpCompleted, "Deletion of temporary data for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}.", () =>
                    {
                        fulfilmentCleanUpService.DeleteBatchFolder(batch);
                        return true;
                    },
                    batch.BatchId, batch.CorrelationId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.FulfilmentBatchCleanUpFailed.ToEventId(), ex, "Exception occurred while cleaning up temporary data for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}. Exception:{Message}", batch.BatchId, batch.CorrelationId, ex.Message);
                Agent.Tracer.CurrentTransaction?.CaptureException(ex);
            }
            finally
            {
                Agent.Tracer.CurrentTransaction?.End();
            }
        }
    }
}
