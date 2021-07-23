using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger, IFileSystemHelper fileSystemHelper)
        {
            this.configuration = configuration;
            this.fulFilmentDataService = fulFilmentDataService;
            this.logger = logger;
            this.fileSystemHelper = fileSystemHelper;
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
                string errorFileName = "error.txt";
                var exchangeSetBatchFolderPath = Path.Combine(homeDirectoryPath, currentUtcDateTime, fulfilmentServiceQueueMessage.BatchId);
                fileSystemHelper.CheckAndCreateFolder(exchangeSetBatchFolderPath);

                var errorFilePath = Path.Combine(exchangeSetBatchFolderPath, errorFileName);
                await File.WriteAllTextAsync(errorFilePath, ex.ErrorMessage);
            }
        }
    }
}
