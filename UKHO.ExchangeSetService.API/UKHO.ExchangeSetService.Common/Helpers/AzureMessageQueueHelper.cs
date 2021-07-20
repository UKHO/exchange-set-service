using Azure.Storage.Queues;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureMessageQueueHelper : IAzureMessageQueueHelper
    {
        public async Task AddMessage(IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration, int instanceCount, string storageAccountConnectionString, string message)
        {
            var queue = "ess-{0}-fulfilment";
            // Create the queue client.
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, string.Format(queue, instanceCount));

            // convert message to base64string          
            var messageBase64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
        }
    }
}
