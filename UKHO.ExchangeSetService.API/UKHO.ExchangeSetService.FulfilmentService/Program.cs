using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.ExchangeSetService.FulfilmentService.Services;

namespace UKHO.ExchangeSetService.FulfilmentService
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static IServiceProvider ServiceProvider;
        private static IConfiguration ConfigurationBuilder;
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
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                    builder.AddAzureKeyVault(kvServiceUri,
                                             keyVaultClient,
                                             new DefaultKeyVaultSecretManager());
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

                 builder.AddConsole();

                 //Add Application Insights if needed (if key exists in settings)
                 string instrumentationKey = ConfigurationBuilder["APPINSIGHTS_INSTRUMENTATIONKEY"];
                 if (!string.IsNullOrEmpty(instrumentationKey))
                 {
                     builder.AddApplicationInsightsWebJobs(o => o.InstrumentationKey = instrumentationKey);
                 }

                 var eventhubConfig = ConfigurationBuilder.GetSection("EventHubLoggingConfiguration");

             })
             .ConfigureServices((hostContext, services) =>
             {
                 Program.ServiceProvider = services.BuildServiceProvider();

                 services.Configure<EssFulfilmentStorageConfiguration>(ConfigurationBuilder.GetSection("EssFulfilmentStorageConfiguration"));
                 services.Configure<QueuesOptions>(ConfigurationBuilder.GetSection("QueuesOptions"));

                 services.AddScoped<IEssFulfilmentStorageConfiguration, EssFulfilmentStorageConfiguration>();
                 services.AddScoped<IScsStorageService, ScsStorageService>();
                 services.AddScoped<IFulfilmentDataService, FulfilmentDataService>();
                 services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
                 services.AddScoped<IAzureMessageQueueHelper, AzureMessageQueueHelper>();
                 services.AddHttpClient<IFileShareServiceClient, FileShareServiceClient>(client =>
                    client.BaseAddress = new Uri(ConfigurationBuilder["FileShareService:BaseUrl"])
                 );
                 services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();
                 services.AddScoped<IFileShareService, FileShareService>();
                 services.AddScoped<IQueryFssService, QueryFssService>();
                 services.Configure<FileShareServiceConfiguration>(ConfigurationBuilder.GetSection("FileShareService"));
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
