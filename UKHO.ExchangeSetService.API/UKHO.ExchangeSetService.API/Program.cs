using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Claims;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.API.Validation.V2;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.HealthCheck;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Storage;
using UKHO.Logging.EventHubLogProvider;

namespace UKHO.ExchangeSetService.API
{
    [ExcludeFromCodeCoverage]
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddEnvironmentVariables();
            string kvServiceUri = builder.Configuration["KeyVaultSettings:ServiceUri"];

            if (!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                builder.Configuration.AddAzureKeyVault(new Uri(kvServiceUri),
                    new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = builder.Configuration["ESSManagedIdentity:ClientId"] }));
            }

#if DEBUG
            builder.Configuration.AddJsonFile("appsettings.local.overrides.json", true, true);
            //Add file based logger for development
            builder.Logging.AddFile(builder.Configuration.GetSection("Logging"));
#endif

            // Add services to the container.
            builder.Logging.AddAzureWebAppDiagnostics();
            builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddControllers(o =>
            {
                o.AllowEmptyInputInBodyModelBinding = true;
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            builder.Services.Configure<EventHubLoggingConfiguration>(builder.Configuration.GetSection("EventHubLoggingConfiguration"));

            var essAzureADConfiguration = new AzureADConfiguration();
            builder.Configuration.Bind("ESSAzureADConfiguration", essAzureADConfiguration);

            var azureAdB2CConfiguration = new AzureAdB2CConfiguration();
            builder.Configuration.Bind("AzureAdB2CConfiguration", azureAdB2CConfiguration);

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer("AzureAD", options =>
                    {
                        options.Audience = essAzureADConfiguration.ClientId;
                        options.Authority = $"{essAzureADConfiguration.MicrosoftOnlineLoginUrl}{essAzureADConfiguration.TenantId}";
                    })
                    .AddJwtBearer("AzureB2C", jwtOptions =>
                    {
                        jwtOptions.Audience = azureAdB2CConfiguration.ClientId;
                        jwtOptions.Authority = $"{azureAdB2CConfiguration.Instance}{azureAdB2CConfiguration.Domain}/{azureAdB2CConfiguration.SignUpSignInPolicy}/v2.0/";
                    })
                    .AddJwtBearer("AzureADB2C", options =>
                    {
                        options.Audience = azureAdB2CConfiguration.ClientId;
                        options.Authority = $"{essAzureADConfiguration.MicrosoftOnlineLoginUrl}{azureAdB2CConfiguration.TenantId}";
                        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidAudience = azureAdB2CConfiguration.ClientId,
                            ValidIssuer = $"{essAzureADConfiguration.MicrosoftOnlineLoginUrl}{azureAdB2CConfiguration.TenantId}/v2.0"
                        };
                    });

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("AzureAD", "AzureB2C", "AzureADB2C")
                .Build();
            });

            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            builder.Services.Configure<EssFulfilmentStorageConfiguration>(builder.Configuration.GetSection("ESSFulfilmentConfiguration"));
            builder.Services.Configure<CacheConfiguration>(builder.Configuration.GetSection("CacheConfiguration"));
            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddSingleton<IAuthFssTokenProvider, AuthFssTokenProvider>();
            builder.Services.AddSingleton<IAuthScsTokenProvider, AuthScsTokenProvider>();
            builder.Services.AddScoped<ISalesCatalogueService, SalesCatalogueService>();
            builder.Services.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
            builder.Services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            builder.Services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
            builder.Services.AddScoped<IAzureMessageQueueHelper, AzureMessageQueueHelper>();
            builder.Services.AddScoped<IAzureTableStorageClient, AzureTableStorageClient>();
            builder.Services.AddScoped<IFileShareServiceCache, FileShareServiceCache>();
            builder.Services.AddScoped<IAzureAdB2CHelper, AzureAdB2CHelper>();
            builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddAllElasticApm();

            builder.Services.AddHeaderPropagation(options =>
            {
                options.Headers.Add(CorrelationIdMiddleware.XCorrelationIdHeaderKey);
            });

            builder.Services.Configure<SalesCatalogueConfiguration>(builder.Configuration.GetSection("SalesCatalogue"));

            var retryCount = Convert.ToInt32(builder.Configuration["RetryConfiguration:RetryCount"]);
            var sleepDuration = Convert.ToDouble(builder.Configuration["RetryConfiguration:SleepDuration"]);

            const string ExchangeSetService = "ExchangeSetService";

            builder.Services.AddHttpClient<ISalesCatalogueClient, SalesCatalogueClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["SalesCatalogue:BaseUrl"]);
                var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetService,
                                        Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version);
                client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
            }).AddHeaderPropagation().AddPolicyHandler((services, request) =>
                    CommonHelper.GetRetryPolicy(services.GetService<ILogger<ISalesCatalogueClient>>(), "Sales Catalogue", EventIds.RetryHttpClientSCSRequest, retryCount, sleepDuration));


            builder.Services.Configure<FileShareServiceConfiguration>(builder.Configuration.GetSection("FileShareService"));
            builder.Services.Configure<EssManagedIdentityConfiguration>(builder.Configuration.GetSection("ESSManagedIdentity"));
            builder.Services.Configure<AzureAdB2CConfiguration>(builder.Configuration.GetSection("AzureAdB2CConfiguration"));
            builder.Services.Configure<AzureADConfiguration>(builder.Configuration.GetSection("ESSAzureADConfiguration"));
            builder.Services.Configure<AioConfiguration>(builder.Configuration.GetSection("AioConfiguration"));

            builder.Services.AddHttpClient<IFileShareServiceClient, FileShareServiceClient>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["FileShareService:BaseUrl"]);
                var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetService,
                                            Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version);
                client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
            }).AddHeaderPropagation().AddPolicyHandler((services, request) =>
                    CommonHelper.GetRetryPolicy(services.GetService<ILogger<IFileShareServiceClient>>(), "File Share", EventIds.RetryHttpClientFSSRequest, retryCount, sleepDuration));


            builder.Services.AddScoped<IFileSystemHelper, FileSystemHelper>();
            builder.Services.AddScoped<IFileShareService, FileShareService>();
            builder.Services.AddScoped<IProductDataService, ProductDataService>();
            builder.Services.AddScoped<IMonitorHelper, MonitorHelper>();
            builder.Services.AddScoped<IProductIdentifierValidator, ProductIdentifierValidator>();
            builder.Services.AddScoped<IProductDataProductVersionsValidator, ProductDataProductVersionsValidator>();
            builder.Services.AddScoped<IProductDataSinceDateTimeValidator, ProductDataSinceDateTimeValidator>();
            builder.Services.AddScoped<IExchangeSetStorageProvider, ExchangeSetStorageProvider>();
            builder.Services.AddScoped<IEventHubLoggingHealthClient, EventHubLoggingHealthClient>();
            builder.Services.AddSingleton<ISmallExchangeSetInstance, SmallExchangeSetInstance>();
            builder.Services.AddSingleton<IMediumExchangeSetInstance, MediumExchangeSetInstance>();
            builder.Services.AddSingleton<ILargeExchangeSetInstance, LargeExchangeSetInstance>();
            builder.Services.AddScoped<IAzureWebJobsHealthCheckClient, AzureWebJobsHealthCheckClient>();
            builder.Services.AddScoped<IAzureWebJobsHealthCheckService, AzureWebJobsHealthCheckService>();
            builder.Services.AddSingleton<IWebJobsAccessKeyProvider>(s => new WebJobsAccessKeyProvider(builder.Configuration));
            builder.Services.AddScoped<UserIdentifier>();
            builder.Services.AddScoped<IFileSystem, FileSystem>();
            builder.Services.AddScoped<BespokeExchangeSetAuthorizationFilterAttribute>();
            builder.Services.AddScoped<IScsProductIdentifierValidator, ScsProductIdentifierValidator>();
            builder.Services.AddScoped<IScsDataSinceDateTimeValidator, ScsDataSinceDateTimeValidator>();
            builder.Services.AddScoped<IExchangeSetStandardService, ExchangeSetStandardService>();
            builder.Services.AddScoped<IProductNameValidator, ProductNameValidator>();
            builder.Services.AddScoped<IUpdatesSinceValidator, UpdatesSinceValidator>();
            builder.Services.AddScoped<IProductVersionsValidator, ProductVersionsValidator>();

            builder.Services.AddHealthChecks()
                .AddCheck<FileShareServiceHealthCheck>("FileShareServiceHealthCheck")
                .AddCheck<SalesCatalogueServiceHealthCheck>("SalesCatalogueServiceHealthCheck")
                .AddCheck<EventHubLoggingHealthCheck>("EventHubLoggingHealthCheck")
                .AddCheck<AzureBlobStorageHealthCheck>("AzureBlobStorageHealthCheck")
                .AddCheck<AzureMessageQueueHealthCheck>("AzureMessageQueueHealthCheck")
                .AddCheck<AzureWebJobsHealthCheck>("AzureWebJobsHealthCheck");
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddScoped<IEnterpriseEventCacheDataRequestValidator, EnterpriseEventCacheDataRequestValidator>();
            builder.Services.AddScoped<IEssWebhookService, EssWebhookService>();

            builder.Services.AddEndpointsApiExplorer();
            ConfigureSwagger();

            var app = builder.Build();
            ConfigureLogging(app);

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "UKHO Exchange Set Server APIs");
                c.RoutePrefix = "swagger";
            });

            app.UseHttpsRedirection();
            app.UseHsts(x => x.MaxAge(365).IncludeSubdomains());
            app.UseReferrerPolicy(x => x.NoReferrer());
            app.UseCsp(x => x.DefaultSources(y => y.Self()));
            app.UsePermissionsPolicyHeader();
            app.UseXfo(x => x.SameOrigin());
            app.UseXContentTypeOptions();
            app.UseHeaderPropagation();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHealthChecks("/health");
            app.MapControllers();
            app.Run();

            //=====================================
            void ConfigureSwagger()
            {
                var swaggerConfiguration = new SwaggerConfiguration();
                builder.Configuration.Bind("Swagger", swaggerConfiguration);
                builder.Services.AddSwaggerGenNewtonsoftSupport();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = swaggerConfiguration.Version,
                        Title = swaggerConfiguration.Title,
                        Description = swaggerConfiguration.Description,
                        Contact = new OpenApiContact
                        {
                            Email = swaggerConfiguration.Email,
                        },
                    });
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                    c.EnableAnnotations();
                    c.OperationFilter<AddResponseHeadersFilter>();
                    c.AddSecurityDefinition("jwtBearerAuth", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT"
                    });
                    c.OperationFilter<UKHO.ExchangeSetService.API.Filters.SecurityRequirementsOperationFilter>();
                });
            }

            void ConfigureLogging(WebApplication app)
            {
                var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
                var eventHubLoggingConfiguration = app.Services.GetRequiredService<IOptions<EventHubLoggingConfiguration>>();
                var httpContextAccessor = app.Services.GetRequiredService<IHttpContextAccessor>();

                if (!string.IsNullOrWhiteSpace(eventHubLoggingConfiguration?.Value.ConnectionString))
                {
                    void ConfigAdditionalValuesProvider(IDictionary<string, object> additionalValues)
                    {
                        if (httpContextAccessor.HttpContext != null)
                        {
                            additionalValues["_RemoteIPAddress"] = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
                            additionalValues["_User-Agent"] = httpContextAccessor.HttpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
                            additionalValues["_AssemblyVersion"] = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version;
                            additionalValues["_X-Correlation-ID"] =
                                httpContextAccessor.HttpContext.Request.Headers?[CorrelationIdMiddleware.XCorrelationIdHeaderKey].FirstOrDefault() ?? string.Empty;

                            if (httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                                additionalValues["_UserId"] = httpContextAccessor.HttpContext.User.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
                        }
                    }

                    loggerFactory.AddEventHub(
                                             config =>
                                             {
                                                 config.Environment = eventHubLoggingConfiguration.Value.Environment;
                                                 config.DefaultMinimumLogLevel =
                                                     (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.MinimumLoggingLevel, true);
                                                 config.MinimumLogLevels["UKHO"] =
                                                     (LogLevel)Enum.Parse(typeof(LogLevel), eventHubLoggingConfiguration.Value.UkhoMinimumLoggingLevel, true);
                                                 config.EventHubConnectionString = eventHubLoggingConfiguration.Value.ConnectionString;
                                                 config.EventHubEntityPath = eventHubLoggingConfiguration.Value.EntityPath;
                                                 config.System = eventHubLoggingConfiguration.Value.System;
                                                 config.Service = eventHubLoggingConfiguration.Value.Service;
                                                 config.NodeName = eventHubLoggingConfiguration.Value.NodeName;
                                                 config.AdditionalValuesProvider = ConfigAdditionalValuesProvider;
                                             });
                }

                app.UseLogAllRequestsAndResponses(loggerFactory);

                app.UseCorrelationIdMiddleware()
                   .UseErrorLogging(loggerFactory);
            }
        }
    }
}
