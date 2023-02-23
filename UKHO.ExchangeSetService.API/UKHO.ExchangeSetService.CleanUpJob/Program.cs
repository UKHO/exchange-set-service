using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
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
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace UKHO.ExchangeSetService.CleanUpJob
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        static InMemoryChannel aiChannel = new InMemoryChannel();

        static async Task Main()
        {
            try
            {
                var delayTime = 5000;

                //Build configuration
                var configuration = BuildConfiguration();

                var serviceCollection = new ServiceCollection();

                //Configure required services
                ConfigureServices(serviceCollection, configuration);

                //Create service provider. This will be used in logging.
                var serviceProvider = serviceCollection.BuildServiceProvider();

                try
                {
                    await serviceProvider.GetService<ExchangeSetCleanUpJob>().ProcessCleanUp();
                }
                finally
                {
                    //Ensure all buffered app insights logs are flushed into Azure
                    aiChannel.Flush();
                    await Task.Delay(delayTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}{Environment.NewLine}Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static IConfigurationRoot BuildConfiguration()
        {
            var configBuilder =
                new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true);

            //Add environment specific configuration files.
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environmentName))
            {
                configBuilder.AddJsonFile($"appsettings.{environmentName}.json", optional: true);
            }

            var tempConfig = configBuilder.Build();
            string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];
            if (!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                var secretClient = new SecretClient(new Uri(kvServiceUri), new DefaultAzureCredential(
                                                    new DefaultAzureCredentialOptions { ManagedIdentityClientId = tempConfig["ESSManagedIdentity:ClientId"] }));
                configBuilder.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());
            }
            #if DEBUG
            //Add development overrides configuration
            configBuilder.AddJsonFile("appsettings.local.overrides.json", true, true);
            #endif

            //Add environment variables
            configBuilder.AddEnvironmentVariables();

            return configBuilder.Build();
        }

        private static void ConfigureServices(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            //Add logging
            serviceCollection.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

                string instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrEmpty(instrumentationKey))
                {
                    loggingBuilder.AddApplicationInsights(
                        configureTelemetryConfiguration: (config) => config.ConnectionString = instrumentationKey,
                        configureApplicationInsightsLoggerOptions: (options) => { }
                        );
                }

                #if DEBUG
                loggingBuilder.AddSerilog(new LoggerConfiguration()
                                .WriteTo.File("Logs/UKHO.ExchangeSetService.CleanUpLogs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                .MinimumLevel.Information()
                                .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                .CreateLogger(), dispose: true);
                #endif

                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();

                EventHubLoggingConfiguration eventhubConfig = configuration.GetSection("EventHubLoggingConfiguration").Get<EventHubLoggingConfiguration>();

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
            
            serviceCollection.Configure<TelemetryConfiguration>(
                (config) =>
                {
                    config.TelemetryChannel = aiChannel;
                    config.ConnectionString = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                }
            );

            serviceCollection.Configure<EssFulfilmentStorageConfiguration>(configuration.GetSection("EssFulfilmentStorageConfiguration"));
            serviceCollection.Configure<CleanUpConfiguration>(configuration.GetSection("CleanUpConfiguration"));

            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddTransient<ExchangeSetCleanUpJob>();
            serviceCollection.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
            serviceCollection.AddScoped<IExchangeSetCleanUpService, ExchangeSetCleanUpService>();
            serviceCollection.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
            serviceCollection.AddScoped<IAzureFileSystemHelper, AzureFileSystemHelper>();
        }
    }
}
