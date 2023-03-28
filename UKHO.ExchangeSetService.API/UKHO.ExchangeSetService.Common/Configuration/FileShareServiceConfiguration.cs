namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class FileShareServiceConfiguration
    {
        public string BaseUrl { get; set; }
        public string PublicBaseUrl { get; set; }
        public string ResourceId { get; set; }
        public string BusinessUnit { get; set; }
        public string ExchangeSetFileName { get; set; }
        public string ExchangeSetFileFolder { get; set; }
        public string EncRoot { get; set; }
        public string ReadMeFileName { get; set; }
        public string Info { get; set; }
        public string ProductFileName { get; set; }
        public string BaseCellExtension { get; set; }
        public int Limit { get; set; }
        public int Start { get; set; }
        public string ProductCode { get; set; }
        public string CellName { get; set; }
        public string EditionNumber { get; set; }
        public string UpdateNumber { get; set; }
        public int UpdateNumberLimit { get; set; }
        public int ProductLimit { get; set; }
        public int ParallelSearchTaskCount { get; set; }
        public string ProductType { get; set; }
        public string SerialFileName { get; set; }
        public string SerialAioFileName { get; set; }
        public int BlockSizeInMultipleOfKBs { get; set; }
        public int ParallelUploadThreadCount { get; set; }
        public string CatalogFileName { get; set; }
        public string CommentVersion { get; set; }
        public int BatchCommitCutOffTimeInMinutes { get; set; }
        public int BatchCommitDelayTimeInMilliseconds { get; set; }
        public int PosBatchCommitCutOffTimeInMinutes { get; set; }
        public int PosBatchCommitDelayTimeInMilliseconds { get; set; }
        public string ErrorFileName { get; set; }
        public string ContentInfo { get; set; }
        public string Content { get; set; }
        public string Adc { get; set; }
        public string AioExchangeSetFileName { get; set; }
        public string AioExchangeSetFileFolder { get; set; }
    }
}