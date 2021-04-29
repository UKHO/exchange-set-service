using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.API.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SwaggerConfiguration
    {
        public string Version { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Email { get; set; }
    }
}
