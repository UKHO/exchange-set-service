using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
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
        public async Task AddMessage(string batchId, int instanceNumber, string storageAccountConnectionString, string message, string correlationId)
        {
            var queue = string.Format(essFulfilmentStorageconfig.Value.DynamicQueueName, instanceNumber);
            // Create the queue client.
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, queue);

            // convert message to base64string          
            var messageBase64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
            logger.LogInformation(EventIds.AddedMessageInQueue.ToEventId(), "Added message in Queue:{queue} for BatchId:{batchId} and _X-Correlation-ID:{CorrelationId}", queue, batchId, correlationId);
        }

        public async Task<HealthCheckResult> CheckMessageQueueHealth(string storageAccountConnectionString, string queueName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);
            var queueMessageExists = await queue.ExistsAsync();
            if (queueMessageExists)
                return HealthCheckResult.Healthy("Azure message queue is healthy");
            else
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy", new Exception($"Azure message queue {queueName} does not exists"));
        }
    }
}
