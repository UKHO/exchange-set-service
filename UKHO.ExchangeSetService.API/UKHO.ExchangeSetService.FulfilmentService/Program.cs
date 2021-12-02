using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UKHO.Logging.EventHubLogProvider;
#pragma warning disable S1128 // Unused "using" should be removed
using Serilog;
using Serilog.Events;
#pragma warning disable S1128 // Unused "using" should be removed
using System;
using System.Diagnostics.CodeAnalysis;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;
using System.Reflection;
using System.Linq;
using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using UKHO.ExchangeSetService.FulfilmentService.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using UKHO.ExchangeSetService.FulfilmentService.Filters;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static IConfiguration ConfigurationBuilder;
        private static string AssemblyVersion = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
        public const string ExchangeSetServiceUserAgent = "ExchangeSetService";
       
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
                                 .WriteTo.File("Logs/UKHO.ExchangeSetService.FulfilmentServiceLogs-.txt", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] [{SourceContext}] {Message}{NewLine}{Exception}")
                                 .MinimumLevel.Information()
                                 .MinimumLevel.Override("UKHO", LogEventLevel.Debug)
                                 .CreateLogger(), dispose: true);
                 #endif

                 builder.AddConsole();

                 //Add Application Insights if needed (if key exists in settings)
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
                 var buildServiceProvider = services.BuildServiceProvider();

                 services.Configure<EssFulfilmentStorageConfiguration>(ConfigurationBuilder.GetSection("EssFulfilmentStorageConfiguration"));
                 services.Configure<QueuesOptions>(ConfigurationBuilder.GetSection("QueuesOptions"));
                 services.Configure<SalesCatalogueConfiguration>(ConfigurationBuilder.GetSection("SalesCatalogue"));
         
                 services.AddScoped<IEssFulfilmentStorageConfiguration, EssFulfilmentStorageConfiguration>();
                 services.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
                 services.AddScoped<IFulfilmentDataService, FulfilmentDataService>();
                 services.AddScoped<IMonitorHelper, MonitorHelper>();
                 services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
                 services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
                 services.AddScoped<IAzureMessageQueueHelper, AzureMessageQueueHelper>();

                 var retryCount = Convert.ToInt32(ConfigurationBuilder["RetryConfiguration:RetryCount"]);
                 var sleepDuration = Convert.ToDouble(ConfigurationBuilder["RetryConfiguration:SleepDuration"]);
                 services.AddHttpClient<IFileShareServiceClient, FileShareServiceClient>(client =>
                 {
                     client.BaseAddress = new Uri(ConfigurationBuilder["FileShareService:BaseUrl"]);                     
                     var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetServiceUserAgent, AssemblyVersion);
                     client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                 })
                 .AddPolicyHandler((services, request) => CommonHelper.GetRetryPolicy(services.GetService<ILogger<IFileShareServiceClient>>(), "File Share", EventIds.RetryHttpClientFSSRequest, retryCount, sleepDuration));
                 
                 services.AddHttpClient<ISalesCatalogueClient, SalesCatalogueClient>(client =>
                 {
                     client.BaseAddress = new Uri(ConfigurationBuilder["SalesCatalogue:BaseUrl"]);
                     var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetServiceUserAgent, AssemblyVersion);
                     client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                 })
                 .AddPolicyHandler((services, request) => CommonHelper.GetRetryPolicy(services.GetService<ILogger<ISalesCatalogueClient>>(), "Sales Catalogue", EventIds.RetryHttpClientSCSRequest, retryCount, sleepDuration));

                 services.AddHttpClient<ICallBackClient, CallBackClient>();

                 services.AddSingleton<IAuthFssTokenProvider, AuthFssTokenProvider>();
                 services.AddSingleton<IAuthScsTokenProvider, AuthScsTokenProvider>();
                 services.AddScoped<IFileShareService, FileShareService>();
                 services.AddScoped<IFulfilmentFileShareService, FulfilmentFileShareService>();
                 services.AddScoped<IFulfilmentAncillaryFiles, FulfilmentAncillaryFiles>();
                 services.AddScoped<IFileSystemHelper, FileSystemHelper>();
                 services.AddScoped<ISalesCatalogueService, SalesCatalogueService>();
                 services.AddScoped<IFulfilmentSalesCatalogueService, FulfilmentSalesCatalogueService>();
                 services.AddSingleton<ISmallExchangeSetInstance, SmallExchangeSetInstance>();
                 services.AddSingleton<IMediumExchangeSetInstance, MediumExchangeSetInstance>();
                 services.AddSingleton<ILargeExchangeSetInstance, LargeExchangeSetInstance>();
                 services.AddScoped<IFulfilmentCallBackService, FulfilmentCallBackService>();

                 services.Configure<FileShareServiceConfiguration>(ConfigurationBuilder.GetSection("FileShareService"));
                 services.Configure<EssManagedIdentityConfiguration>(ConfigurationBuilder.GetSection("ESSManagedIdentity"));
                 services.Configure<EssCallBackConfiguration>(ConfigurationBuilder.GetSection("ESSCallBackConfiguration"));

                 services.AddDistributedMemoryCache();

                 // Add App Insights Telemetry Filter
                 var telemetryConfiguration = buildServiceProvider.GetRequiredService<TelemetryConfiguration>();
                 var telemetryProcessorChainBuilder = telemetryConfiguration.TelemetryProcessorChainBuilder;
                 telemetryProcessorChainBuilder.Use(next => new AzureDependencyFilterTelemetryProcessor(next));
                 telemetryProcessorChainBuilder.Build();

             })
              .ConfigureWebJobs(b =>
              {
                  b.AddAzureStorageCoreServices();
                  b.AddAzureStorage();
              });

            return hostBuilder;
        }
    }
}