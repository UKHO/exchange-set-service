using Azure.Storage.Queues;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
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
    }
}
