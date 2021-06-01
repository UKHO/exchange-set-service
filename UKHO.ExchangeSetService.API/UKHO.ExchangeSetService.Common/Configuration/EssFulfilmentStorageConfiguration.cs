namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class EssFulfilmentStorageConfiguration : IEssFulfilmentStorageConfiguration
    {
        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string StorageContainerName { get; set; }
        public string QueueName { get; set; }
    }
}
