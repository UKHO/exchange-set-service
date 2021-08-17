using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.Logging.EventHubLogProvider;
using Azure.Identity;
using UKHO.ExchangeSetService.Common.Helpers;
using System.Net.Http.Headers;
using UKHO.ExchangeSetService.Common.Storage;
using Microsoft.AspNetCore.Authorization;
using UKHO.ExchangeSetService.Common.HealthCheck;

namespace UKHO.ExchangeSetService.API
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfiguration configuration;
        public const string ExchangeSetService = "ExchangeSetService";

        public Startup(IWebHostEnvironment env)
        {
            this.configuration = this.BuildConfiguration(env);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Enables Application Insights telemetry.
            services.AddApplicationInsightsTelemetry();
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));
                loggingBuilder.AddConsole();
                loggingBuilder.AddDebug();
                loggingBuilder.AddAzureWebAppDiagnostics();
            });

            services.AddControllers(o =>
            {
                o.AllowEmptyInputInBodyModelBinding = true;
            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });
            this.ConfigureSwagger(services);

            var essAzureADConfiguration = new AzureADConfiguration();
            configuration.Bind("ESSAzureADConfiguration", essAzureADConfiguration);

            var azureAdB2CConfiguration = new AzureAdB2CConfiguration();
            configuration.Bind("AzureAdB2CConfiguration", azureAdB2CConfiguration);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes("AzureAD", "AzureB2C", "AzureADB2C")               
                .Build();
            });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.Configure<EssFulfilmentStorageConfiguration>(configuration.GetSection("ESSFulfilmentConfiguration"));
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();
            services.AddScoped<ISalesCatalogueService, SalesCatalogueService>();
            services.AddScoped<ISalesCatalogueStorageService, SalesCatalogueStorageService>();
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            services.AddScoped<IAzureBlobStorageClient, AzureBlobStorageClient>();
            services.AddScoped<IAzureMessageQueueHelper, AzureMessageQueueHelper>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.AddHeaderPropagation(options =>
            {
                options.Headers.Add(CorrelationIdMiddleware.XCorrelationIdHeaderKey);
            });

            services.Configure<SalesCatalogueConfiguration>(configuration.GetSection("SalesCatalogue"));

            services.AddHttpClient<ISalesCatalogueClient, SalesCatalogueClient>(client =>
                {
                    client.BaseAddress = new Uri(configuration["SalesCatalogue:BaseUrl"]);
                    var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetService,
                                            Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version);
                    client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                }
            )
            .AddHeaderPropagation();

            services.Configure<FileShareServiceConfiguration>(configuration.GetSection("FileShareService"));
            services.Configure<EssManagedIdentityConfiguration>(configuration.GetSection("ESSManagedIdentity"));
            services.Configure<AzureAdB2CConfiguration>(configuration.GetSection("AzureAdB2CConfiguration"));
            services.Configure<AzureADConfiguration>(configuration.GetSection("ESSAzureADConfiguration"));

            services.AddHttpClient<IFileShareServiceClient, FileShareServiceClient>(client =>
                {
                    client.BaseAddress = new Uri(configuration["FileShareService:BaseUrl"]);
                    var productHeaderValue = new ProductInfoHeaderValue(ExchangeSetService,
                                                Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyFileVersionAttribute>().Single().Version);
                    client.DefaultRequestHeaders.UserAgent.Add(productHeaderValue);
                }
            )
            .AddHeaderPropagation();
            services.AddScoped<IFileSystemHelper, FileSystemHelper>();
            services.AddScoped<IFileShareService, FileShareService>();
            services.AddScoped<IProductDataService, ProductDataService>();
            services.AddScoped<IProductIdentifierValidator, ProductIdentifierValidator>();
            services.AddScoped<IProductDataProductVersionsValidator, ProductDataProductVersionsValidator>();
            services.AddScoped<IProductDataSinceDateTimeValidator, ProductDataSinceDateTimeValidator>();
            services.AddScoped<IExchangeSetStorageProvider, ExchangeSetStorageProvider>();
            services.AddScoped<IEventHubLoggingHealthClient, EventHubLoggingHealthClient>();
            services.AddSingleton<ISmallExchangeSetInstance, SmallExchangeSetInstance>();
            services.AddSingleton<IMediumExchangeSetInstance, MediumExchangeSetInstance>();
            services.AddSingleton<ILargeExchangeSetInstance, LargeExchangeSetInstance>();
            services.AddScoped<IAzureWebJobsHealthCheckClient, AzureWebJobsHealthCheckClient>();
            services.AddSingleton<IWebJobsAccessKeyProvider>(s => new WebJobsAccessKeyProvider(configuration));

            services.AddHealthChecks()
                .AddCheck<FileShareServiceHealthCheck>("FileShareServiceHealthCheck")
                .AddCheck<SalesCatalogueServiceHealthCheck>("SalesCatalogueServiceHealthCheck")
                .AddCheck<EventHubLoggingHealthCheck>("EventHubLoggingHealthCheck")
                .AddCheck<AzureBlobStorageHealthCheck>("AzureBlobStorageHealthCheck")
                .AddCheck<AzureMessageQueueHealthCheck>("AzureMessageQueueHealthCheck")
                .AddCheck<AzureWebJobsHealthCheck>("AzureWebJobsHealthCheck");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory,
                            IHttpContextAccessor httpContextAccessor, IOptions<EventHubLoggingConfiguration> eventHubLoggingConfiguration)
        {
            ConfigureLogging(app, loggerFactory, httpContextAccessor, eventHubLoggingConfiguration);

            if (env.IsDevelopment())
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

            app.UseHeaderPropagation();

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHealthChecks("/health");
            });
        }

        protected IConfigurationRoot BuildConfiguration(IWebHostEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true, true);

            builder.AddEnvironmentVariables();

            var tempConfig = builder.Build();
            string kvServiceUri = tempConfig["KeyVaultSettings:ServiceUri"];

            if (!string.IsNullOrWhiteSpace(kvServiceUri))
            {
                builder.AddAzureKeyVault(new Uri(kvServiceUri),
                    new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = tempConfig["ESSManagedIdentity:ClientId"] }));
            }

#if DEBUG
            builder.AddJsonFile("appsettings.local.overrides.json", true, true);
#endif
            return builder.Build();
        }

        private void ConfigureSwagger(IServiceCollection services)
        {
            var swaggerConfiguration = new SwaggerConfiguration();
            this.configuration.Bind("Swagger", swaggerConfiguration);
            services.AddSwaggerGen(c =>
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
                c.OperationFilter<Filters.SecurityRequirementsOperationFilter>();
            });

            services.Configure<EventHubLoggingConfiguration>(configuration.GetSection("EventHubLoggingConfiguration"));
        }

        [SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "httpContextAccessor is used in action delegate")]
        private void ConfigureLogging(IApplicationBuilder app, ILoggerFactory loggerFactory,
                                    IHttpContextAccessor httpContextAccessor, IOptions<EventHubLoggingConfiguration> eventHubLoggingConfiguration)
        {
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
#if (DEBUG)
            //Add file based logger for development
            loggerFactory.AddFile(configuration.GetSection("Logging"));
#endif
            app.UseLogAllRequestsAndResponses(loggerFactory);

            app.UseCorrelationIdMiddleware()
               .UseErrorLogging(loggerFactory);
        }
    }
}
