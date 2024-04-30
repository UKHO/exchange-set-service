namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class Storage2CacheConfiguration
    {
        public string CacheStorage2AccountName { get; set; }
        public string CacheStorage2AccountKey { get; set; }
        public string FssSearchCache2TableName { get; set; }
        public bool IsFssCacheEnabled { get; set; }
        public string Cache2BusinessUnit { get; set; }
        public string Cache2ProductCode { get; set; }
    }
}
