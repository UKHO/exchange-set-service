using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureMessageQueueHelper : IAzureMessageQueueHelper
    {
        private readonly ILogger<AzureMessageQueueHelper> logger;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;

        public AzureMessageQueueHelper(ILogger<AzureMessageQueueHelper> logger, IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            this.logger = logger;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
        }
        public async Task AddMessage(string batchId, int instanceCount, string storageAccountConnectionString, string message, string correlationId)
        {
            var queue = string.Format(essFulfilmentStorageconfig.Value.DynamicQueueName, instanceCount);
            // Create the queue client.
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, queue);

            // convert message to base64string          
            var messageBase64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
            logger.LogInformation(EventIds.AddedMessageInQueueSCSResponseStored.ToEventId(), "Added message in Queue:{queue} with Sales catalogue response uri for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", queue, batchId, correlationId);
        }
    }
}
