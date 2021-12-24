namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class FssCacheConfiguration
    {
        public string FssCacheStorageAccountName { get; set; }
        public string FssCacheStorageAccountKey { get; set; }
        public string FssCacheTableName { get; set; }
        public bool IsFssCache { get; set; }
    }
}
