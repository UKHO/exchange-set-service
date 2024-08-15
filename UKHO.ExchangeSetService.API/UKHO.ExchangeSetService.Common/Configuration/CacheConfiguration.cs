namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class CacheConfiguration
    {
        public string CacheStorageAccountName { get; set; }
        public string CacheStorageAccountKey { get; set; }
        public string FssSearchCacheTableName { get; set; }
        public bool IsFssCacheEnabled { get; set; }
        public string S63CacheBusinessUnit { get; set; }
        public string S57CacheBusinessUnit { get; set; }
        public string CacheProductCode { get; set; }
    }
}
