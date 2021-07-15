using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class AzureMessageQueueHelper : IAzureMessageQueueHelper
    {
        private readonly ISmallExchangeSetInstance smallExchangeSetInstance;
        private readonly IMediumExchangeSetInstance mediumExchangeSetInstance;
        private readonly ILargeExchangeSetInstance largeExchangeSetInstance;
        private readonly IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig;

        public AzureMessageQueueHelper(ISmallExchangeSetInstance smallExchangeSetInstance,
            IMediumExchangeSetInstance mediumExchangeSetInstance, ILargeExchangeSetInstance largeExchangeSetInstance,
            IOptions<EssFulfilmentStorageConfiguration> essFulfilmentStorageconfig)
        {
            this.smallExchangeSetInstance = smallExchangeSetInstance;
            this.mediumExchangeSetInstance = mediumExchangeSetInstance;
            this.largeExchangeSetInstance = largeExchangeSetInstance;
            this.essFulfilmentStorageconfig = essFulfilmentStorageconfig;
        }

        public async Task AddMessage(IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration, double fileSizeInMB, string message)
        {
            var instanceCountAndType = GetInstanceCountBasedOnFileSize(fileSizeInMB);
            var storageAccountWithKey = GetStorageAccountNameAndKey(instanceCountAndType.Item2);
            string storageAccountConnectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccountWithKey.Item1};AccountKey={storageAccountWithKey.Item2};EndpointSuffix=core.windows.net";
            // Create the queue client.
            QueueClient queueClient = new QueueClient(storageAccountConnectionString, essFulfilmentStorageConfiguration.QueueName.Replace("-1-", "-" + instanceCountAndType.Item1 + "-"));

            // convert message to base64string          
            var messageBase64String = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(message));
            // Send a message to the queue
            await queueClient.SendMessageAsync(messageBase64String);
        }

        private (int, string) GetInstanceCountBasedOnFileSize(double fileSizeInMB)
        {
            if (fileSizeInMB > essFulfilmentStorageconfig.Value.SmallExchangeSetSizeInMB &&
                fileSizeInMB <= essFulfilmentStorageconfig.Value.LargeExchangeSetSizeInMB)
            {
                return (mediumExchangeSetInstance.GetInstanceCount(), ExchangeSetType.MediumExchangeSet.ToString());
            }
            else if (fileSizeInMB > essFulfilmentStorageconfig.Value.LargeExchangeSetSizeInMB)
            {
                return (largeExchangeSetInstance.GetInstanceCount(), ExchangeSetType.LargeExchangeSetInstance.ToString());
            }
            else
            {
                return (smallExchangeSetInstance.GetInstanceCount(), ExchangeSetType.SmallExchangeSet.ToString());
            }
        }

        private (string, string) GetStorageAccountNameAndKey(string exchangeSetType)
        {
            if (string.Compare(exchangeSetType, ExchangeSetType.MediumExchangeSet.ToString(), true) == 0)
            {
                return (essFulfilmentStorageconfig.Value.MediumExchangeSetAccountName, essFulfilmentStorageconfig.Value.MediumExchangeSetAccountKey);
            }
            else if (string.Compare(exchangeSetType, ExchangeSetType.LargeExchangeSetInstance.ToString(), true) == 0)
            {
                return (essFulfilmentStorageconfig.Value.LargeExchangeSetAccountName, essFulfilmentStorageconfig.Value.LargeExchangeSetAccountKey);
            }
            else
            {
                return (essFulfilmentStorageconfig.Value.SmallExchangeSetAccountName, essFulfilmentStorageconfig.Value.SmallExchangeSetAccountKey);
            }
        }
    }
}
