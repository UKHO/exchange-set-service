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
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfig;

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper,
                                    IOptions<FileShareServiceConfiguration> fileShareServiceConfig)
        {
            this.configuration = configuration;
            this.fulFilmentDataService = fulFilmentDataService;
            this.logger = logger;
            this.fileSystemHelper = fileSystemHelper;
            this.fileShareServiceConfig = fileShareServiceConfig;
        }

        public async Task ProcessQueueMessage([QueueTrigger("%QueueName%")] CloudQueueMessage message)
        {
            SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage = JsonConvert.DeserializeObject<SalesCatalogueServiceResponseQueueMessage>(message.AsString);
            string homeDirectoryPath = configuration["HOME"];
            string currentUtcDateTime = DateTime.UtcNow.ToString("ddMMMyyyy");

            try
            {
                logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set web job started for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

                await fulFilmentDataService.CreateExchangeSet(fulfilmentServiceQueueMessage);

                logger.LogInformation(EventIds.CreateExchangeSetRequestCompleted.ToEventId(), "Create Exchange Set web job completed for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
            }
            catch (CustomException ex)
            {
                var uploadErrorFileEventId = EventIds.UploadErrorFile.ToEventId();
                var errorMessage = string.Format(ex.Message, uploadErrorFileEventId.Id, fulfilmentServiceQueueMessage.CorrelationId);

                var exchangeSetBatchFolderPath = Path.Combine(homeDirectoryPath, currentUtcDateTime, fulfilmentServiceQueueMessage.BatchId);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetBatchFolderPath);

                var errorFileFullPath = Path.Combine(exchangeSetBatchFolderPath, fileShareServiceConfig.Value.ErrorFileName);
                fileSystemHelper.CreateFileContent(errorFileFullPath, errorMessage);

                if (fileSystemHelper.CheckFileExists(errorFileFullPath))
                {
                    logger.LogInformation(uploadErrorFileEventId, "Error while processing Exchange Set creation and error file created for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
                }
            }
        }
    }
}
