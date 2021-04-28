using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    [ExcludeFromCodeCoverage]
    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Source} - {Description}";
        }
    }
}
