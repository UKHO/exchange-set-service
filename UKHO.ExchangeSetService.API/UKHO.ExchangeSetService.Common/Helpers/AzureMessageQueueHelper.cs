using Azure.Storage.Queues;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureMessageQueueHelper : IAzureMessageQueueHelper
    {
        public async Task AddMessage(IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration, string message)
        {
            string storageAccountConnectionString = $"DefaultEndpointsProtocol=https;AccountName={essFulfilmentStorageConfiguration.StorageAccountName};AccountKey={essFulfilmentStorageConfiguration.StorageAccountKey};EndpointSuffix=core.windows.net";

            // Create the queue client.
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, essFulfilmentStorageConfiguration.QueueName);

            // convert message to base64string          
            var messageBase64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
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
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy", new Exception("Azure message queue is empty"));
        }
    }
}
