using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
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
            await fulFilmentDataService.BuildExchangeSet(message.BatchId);
            logger.LogInformation(message.BatchId);
        }
    }
}
