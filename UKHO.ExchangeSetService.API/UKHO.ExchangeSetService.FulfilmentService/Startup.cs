using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfigurationRoot configuration;

        public Startup(IWebHostEnvironment hostingEnvironment)
        {
            configuration = BuildConfiguration(hostingEnvironment);
        }

        protected IConfigurationRoot BuildConfiguration(IWebHostEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder();

            builder.AddEnvironmentVariables();

            return builder.Build();
        }

        public void Configure(IApplicationBuilder app)
        {
            throw new System.NotSupportedException();
        }
    }
}
