using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PeriodicOutputServiceConfiguration
    {
        public double LargeMediaExchangeSetSizeInMB { get; set; }
        public string MediaSetFolderName { get; set; }
    }
}
