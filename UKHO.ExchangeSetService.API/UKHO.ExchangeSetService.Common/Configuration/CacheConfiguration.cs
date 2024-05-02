namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class CacheConfiguration
    {
        public string CacheStorageAccountName { get; set; }
        public string CacheStorageAccountKey { get; set; }
        public string CacheStorageAccountName1 { get; set; }
        public string CacheStorageAccountKey1 { get; set; }
        public string CacheStorageAccountName2 { get; set; }
        public string CacheStorageAccountKey2 { get; set; }
        public string FssSearchCacheTableName { get; set; }
        public bool IsFssCacheEnabled { get; set; }
        public string CacheBusinessUnit { get; set; }
        public string CacheProductCode { get; set; }
    }
}
