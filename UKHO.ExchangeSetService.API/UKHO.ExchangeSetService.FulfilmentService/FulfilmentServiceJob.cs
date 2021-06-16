using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;
namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public class FulfilmentServiceJob
    {
        protected IConfiguration ConfigurationBuilder;
        private readonly IFulfilmentDataService fulFilmentDataService;
        private readonly ILogger<FulfilmentServiceJob> logger;

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService, ILogger<FulfilmentServiceJob> logger)
        {
            this.fulFilmentDataService = fulFilmentDataService;
            this.logger = logger;
        }
        public async Task ProcessQueueMessage([QueueTrigger("ess-fulfilment-requests")] CloudQueueMessage message)
        {
            SalesCatalogueServiceResponseQueueMessage fulfilmentServiceQueueMessage = JsonConvert.DeserializeObject<SalesCatalogueServiceResponseQueueMessage>(message.AsString);

            logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set web job started for BatchId:{BatchId} and CorrelationId:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);

            await fulFilmentDataService.CreateExchangeSet(fulfilmentServiceQueueMessage);

            logger.LogInformation(EventIds.CreateExchangeSetRequestCompleted.ToEventId(), "Create Exchange Set web job completed for BatchId:{BatchId} and CorrelationId:{CorrelationId}", fulfilmentServiceQueueMessage.BatchId, fulfilmentServiceQueueMessage.CorrelationId);
        }
    }
}
