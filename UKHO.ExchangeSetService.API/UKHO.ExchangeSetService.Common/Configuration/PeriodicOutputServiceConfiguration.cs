using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PeriodicOutputServiceConfiguration
    {
        public double LargeMediaExchangeSetSizeInMB { get; set; }
        public string LargeExchangeSetFolderName { get; set; }
        public string LargeExchangeSetInfoFolder { get; set; }
        public string LargeExchangeSetAdcFolder { get; set; }
    }
}
