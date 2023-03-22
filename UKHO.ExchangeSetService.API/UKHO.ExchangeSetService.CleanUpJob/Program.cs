using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Diagnostics.CodeAnalysis;
using Serilog;
using Serilog.Events;
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
using System.Threading.Tasks;
//using Microsoft.ApplicationInsights.Channel;
//using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    //[ExcludeFromCodeCoverage]
    public static class Program
    {
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
       
        static async Task Main()
        {
            
            var builder = new HostBuilder();

            builder.ConfigureAppConfiguration((context, configBuilder) => 
            {
                var xx = Environment.GetEnvironmentVariables();
                configBuilder.AddJsonFile("appsettings.json", true, true);

                #if DEBUG
                //Add development overrides configuration
                configBuilder.AddJsonFile("appsettings.local.overrides.json", true, true);
                #endif

                var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (!string.IsNullOrWhiteSpace(environmentName))
                {
                    configBuilder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
                }
                configBuilder.AddEnvironmentVariables();

                var tempConfig = configBuilder.Build();
                string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
                if (!string.IsNullOrWhiteSpace(kvServiceUri))
                {
                    var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                    new DefaultAzureCredentialOptions { ManagedIdentityClientId = context.Configuration["ESSManagedIdentity:ClientId"] }));
                    configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
                }
            });

            builder.ConfigureLogging((context, loggingBuilder) => 
            { 
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();

                string instrumentationKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    loggingBuilder.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = instrumentationKey);
                }

                #if DEBUG
                    loggingBuilder.AddSerilog(new LoggerConfiguration()
                                   .WriteTo.File("Logs/UKHO.ExchangeSetService.CleanUpLogs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                   .MinimumLevel.Information()
                                   .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                   .CreateLogger(), dispose: true);
                #endif


                EventHubLoggingConfiguration eventhubConfig = context.Configuration.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

                if (!string.IsNullOrWhiteSpace(eventhubConfig.ConnectionString))
                {
                    loggingBuilder.AddEventHub(config =>
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
            });

            

            builder.ConfigureServices((context, services) =>
            {
                var buildServiceProvider = services.BuildServiceProvider();

                services.Configure<EssFulfilmentStorageConfiguration>(context.Configuration.GetSection("EssFulfilmentStorageConfiguration"));
                services.Configure<CleanUpConfiguration>(context.Configuration.GetSection("CleanUpConfiguration"));

                services.AddSingleton<IConfiguration>(context.Configuration);
                services.AddTransient<ExchangeSetCleanUpJob>();
                services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
                services.AddScoped<IExchangeSetCleanUpService, ExchangeSetCleanUpService>();
                services.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
                services.AddScoped<IAzureFileSystemHelper, AzureFileSystemHelper>();

            });

            builder.ConfigureWebJobs(b =>
            {
                b.AddAzureStorageCoreServices();
                b.AddAzureStorageQueues();
            });

            var host = builder.Build();
            using (host)
            {
                await host.RunAsync();
            }
        }
        
    }
}
