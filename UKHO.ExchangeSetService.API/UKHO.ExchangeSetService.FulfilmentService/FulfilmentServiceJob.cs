using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        private readonly IFulfilmentDataService fulFilmentDataService;

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService)
        {
            this.fulFilmentDataService = fulFilmentDataService;
        }
        public async Task ProcessQueueMessage([QueueTrigger("ess-fulfilment-requests")] SalesCatalogueServiceResponseQueueMessage message, ILogger logger)
        {
            logger.LogInformation(EventIds.CreateExchangeSetRequestStart.ToEventId(), "Create Exchange Set web job started for {BatchId}", message.BatchId);
            
            await fulFilmentDataService.CreateExchangeSet(message.ScsResponseUri, message.BatchId);
            
            logger.LogInformation(EventIds.CreateExchangeSetRequestCompleted.ToEventId(), "Create Exchange Set web job completed for {BatchId}", message.BatchId);
        }
    }
}
