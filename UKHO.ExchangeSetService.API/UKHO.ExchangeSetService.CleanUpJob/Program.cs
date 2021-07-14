using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics.CodeAnalysis;
#pragma warning disable S1128 // Unused "using" should be removed
using Serilog;
using Serilog.Events;
#pragma warning disable S1128 // Unused "using" should be removed
using Microsoft.Extensions.Logging;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.CleanUpJob.Services;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.Logging.EventHubLogProvider;
using System.Reflection;
using System.Linq;
using UKHO.ExchangeSetService.CleanUpJob.Configuration;
using UKHO.ExchangeSetService.CleanUpJob.Helpers;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static IConfiguration ConfigurationBuilder;
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        public static void Main(string[] args)
        {
            HostBuilder hostBuilder = BuildHostConfiguration();

            IHost host = hostBuilder.Build();

            using (host)
            {
                host.Run();
            }
        }
        private static HostBuilder BuildHostConfiguration()
        {
            HostBuilder hostBuilder = new HostBuilder();
            hostBuilder.ConfigureAppConfiguration((hostContext, builder) =>
            {
                builder.AddJsonFile("appsettings.json");
                //Add environment specific configuration files.
                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    builder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }

                var tempConfig = builder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                        new DefaultAzureCredentialOptions { ManagedIdentityClientId = tempConfig["ESSManagedIdentity:ClientId"] }));
                    builder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }

                #if DEBUG
                //Add development overrides configuration
                builder.AddJsonFile("appsettings.local.overrides.json", true, true);
                #endif

                //Add environment variables
                builder.AddEnvironmentVariables();

                Program.ConfigurationBuilder = builder.Build();
            })
            .ConfigureLogging((hostContext, builder) =>
            {
                builder.AddConfiguration(ConfigurationBuilder.GetSection("Logging"));

                #if DEBUG
                builder.AddSerilog(new LoggerConfiguration()
                                .WriteTo.File("Logs/UKHO.ExchangeSetService.CleanUpLogs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                .MinimumLevel.Information()
                                .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                .CreateLogger(), dispose: true);
                #endif

                builder.AddConsole();

                ////Add Application Insights if needed(if key exists in settings)
                string instrumentationKey = ConfigurationBuilder["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    builder.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = instrumentationKey);
                }

                EventHubLoggingConfiguration eventhubConfig = ConfigurationBuilder.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

                if (!string.IsNullOrWhiteSpace(eventhubConfig.ConnectionString))
                {
                    builder.AddEventHub(config =>
                    {
                        config.Environment = eventhubConfig.Environment;
                        config.DefaultMinimumLogLevel =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventhubConfig.MinimumLoggingLevel, true);
                        config.MinimumLogLevels["UKHO"] =
                            (LogLevel)Enum.Parse(typeof(LogLevel), eventhubConfig.UkhoMinimumLoggingLevel, true);
                        config.EventHubConnectionString = eventhubConfig.ConnectionString;
                        config.EventHubEntityPath = eventhubConfig.EntityPath;
                        config.System = eventhubConfig.System;
                        config.Service = eventhubConfig.Service;
                        config.NodeName = eventhubConfig.NodeName;
                        config.AdditionalValuesProvider = additionalValues =>
                        {
                            additionalValues["_AssemblyVersion"] = AssemblyVersion;
                        };
                    });
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.BuildServiceProvider();

                services.Configure<EssFulfilmentStorageConfiguration>(ConfigurationBuilder.GetSection("EssFulfilmentStorageConfiguration"));
                services.Configure<CleanUpConfig>(ConfigurationBuilder.GetSection("CleanUpConfig"));

                services.AddTransient<ExchangeSetCleanUpJob>();
                services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
                services.AddScoped<IExchangeSetCleanUpService, ExchangeSetCleanUpService>();
                services.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
                services.AddScoped<IAzureFileSystemHelper, AzureFileSystemHelper>();
            })
            .ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddAzureStorage();
                b.AddTimers();
            });
            return hostBuilder;
        }
    }
}
