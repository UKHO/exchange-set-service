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
        public async Task ProcessQueueMessage([QueueTrigger("ess-fulfilment-requests")] ScsResponseQueueMessage message, ILogger logger)
        {

            ScsResponseQueueMessage scsResponseQueueMessage = message;

            await fulFilmentDataService.DownloadSalesCatalogueResponse(scsResponseQueueMessage.ScsResponseUri, scsResponseQueueMessage.BatchId);
            logger.LogInformation(message.BatchId);
        }
    }
}
