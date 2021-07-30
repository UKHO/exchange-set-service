using Azure.Storage.Queues;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, queueName);
            var queueMessage = await queueClient.PeekMessageAsync();
            if (queueMessage != null && queueMessage.GetRawResponse().ReasonPhrase == "OK")
                return HealthCheckResult.Healthy("Azure message queue is healthy");
            else
                return HealthCheckResult.Unhealthy("Azure message queue is unhealthy", new Exception("Azure message queue is empty"));
        }
    }
}
