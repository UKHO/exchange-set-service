namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class UploadMessage
    {
        public long UploadSize { get; set; }       
        public int BlockSizeInMultipleOfKBs { get; set; }
    }
}
