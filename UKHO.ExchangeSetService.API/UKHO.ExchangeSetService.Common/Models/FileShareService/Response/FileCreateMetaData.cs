using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class FileCreateMetaData
    {
        public string AccessToken { get; set; }
        public string BatchId { get; set; }
        public string FileName { get; set; }
        public long Length { get; set; }
    }
}
