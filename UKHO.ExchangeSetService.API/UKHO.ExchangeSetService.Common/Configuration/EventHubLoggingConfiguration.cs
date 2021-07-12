using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class EventHubLoggingConfiguration
    {
        public string MinimumLoggingLevel { get; set; }
        public string UkhoMinimumLoggingLevel { get; set; }
        public string Environment { get; set; }
        public string EntityPath { get; set; }
        public string System { get; set; }
        public string Service { get; set; }
        public string NodeName { get; set; }
        public string ConnectionString { get; set; }
    }
}
