using Azure.Storage.Queues;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    public class FulfilmentServiceJob
    {
        private readonly IFulfilmentDataService fulFilmentDataService;
        private readonly IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration;

        public FulfilmentServiceJob(IConfiguration configuration,
                                    IFulfilmentDataService fulFilmentDataService,
                                    IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration)
        {
            this.fulFilmentDataService = fulFilmentDataService;
            this.essFulfilmentStorageConfiguration = essFulfilmentStorageConfiguration;
        }
        public async Task ProcessQueueMessage([QueueTrigger("ess-fulfilment-requests")] ScsResponseQueueMessage message, ILogger logger)
        {

            ScsResponseQueueMessage scsResponseQueueMessage = message;

            await fulFilmentDataService.DownloadSalesCatalogueResponse(scsResponseQueueMessage.ScsResponseUri, scsResponseQueueMessage.BatchId);
            logger.LogInformation(message.BatchId);
        }
    }
}
