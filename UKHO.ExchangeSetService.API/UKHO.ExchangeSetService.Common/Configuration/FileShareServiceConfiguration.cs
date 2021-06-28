namespace UKHO.ExchangeSetService.Common.Configuration
{
    public class FileShareServiceConfiguration
    {
        public string BaseUrl { get; set; }
        public string ResourceId { get; set; }
        public string BusinessUnit { get; set; }
        public string ExchangeSetFileName { get; set; }
        public string ExchangeSetFileFolder { get; set; }
        public string EncRoot { get; set; }
        public string ReadMeFileName { get; set; }
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
        public int BlockSizeInMultipleOfKBs { get; set; }
        public int ParallelUploadThreadCount { get; set; }
    }
}
