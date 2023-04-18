using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class CustomFileInfo
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public string FullName { get; set; }       
    }
}
