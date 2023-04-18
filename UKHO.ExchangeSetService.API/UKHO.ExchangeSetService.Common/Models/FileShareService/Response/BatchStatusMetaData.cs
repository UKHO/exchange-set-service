using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class BatchStatusMetaData
    {
        public string AccessToken { get; set; }
        public string BatchId { get; set; }
        public string FileName { get; set; }
    }
}
