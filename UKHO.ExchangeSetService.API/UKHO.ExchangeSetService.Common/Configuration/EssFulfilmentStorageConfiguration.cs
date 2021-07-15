namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class EssFulfilmentStorageConfiguration : IEssFulfilmentStorageConfiguration
    {
        public string StorageAccountName { get; set; }
        public string StorageAccountKey { get; set; }
        public string StorageContainerName { get; set; }
        public string SmallExchangeSetAccountName { get; set; }
        public string SmallExchangeSetAccountKey { get; set; }
        public int SmallExchangeSetInstance { get; set; }
        public string MediumExchangeSetAccountName { get; set; }
        public string MediumExchangeSetAccountKey { get; set; }
        public int MediumExchangeSetInstance { get; set; }
        public string LargeExchangeSetAccountName { get; set; }
        public string LargeExchangeSetAccountKey { get; set; }
        public int LargeExchangeSetInstance { get; set; }
        public string QueueName { get; set; }
        public double LargeExchangeSetSizeInMB { get; set; }
        public double SmallExchangeSetSizeInMB { get; set; }
    }
}
