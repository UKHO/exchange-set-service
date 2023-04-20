using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class FileDetail
    {
        public string FileName { get; set; }
        public string Hash { get; set; }
    }
}
