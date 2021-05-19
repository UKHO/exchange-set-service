using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using UKHO.ExchangeSetService.API.Configuration;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Helpers;

namespace UKHO.ExchangeSetService.API
{
    [ExcludeFromCodeCoverage]
    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IWebHostEnvironment env)
        {
            this.configuration = this.BuildConfiguration(env);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
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
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.Audience = essAzureADConfiguration.ClientId;
                        options.Authority = $"{essAzureADConfiguration.MicrosoftOnlineLoginUrl}{essAzureADConfiguration.TenantId}";
                    });

            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthTokenProvider, AuthTokenProvider>();
            services.AddScoped<ISalesCatalogueService, SalesCatalogueService>();
            services.AddAutoMapper(Assembly.GetExecutingAssembly());

            services.Configure<SalesCatalogueConfiguration>(configuration.GetSection("SalesCatalogue"));

            services.AddHttpClient<ISalesCatalogueClient, SalesCatalogueClient>(client =>
                client.BaseAddress = new Uri(configuration["SalesCatalogue:BaseUrl"])
                );

            services.Configure<FileShareServiceConfiguration>(configuration.GetSection("FileShareService"));

            services.AddHttpClient<IFileShareServiceClient, FileShareServiceClient>(client =>
                client.BaseAddress = new Uri(configuration["FileShareService:BaseUrl"])
                );
            services.AddScoped<IFileShareService, FileShareService>();

            services.AddScoped<IProductDataService, ProductDataService>();
            services.AddScoped<IProductIdentifierValidator, ProductIdentifierValidator>();
            services.AddScoped<IProductDataProductVersionsValidator, ProductDataProductVersionsValidator>();
            services.AddScoped<IProductDataSinceDateTimeValidator, ProductDataSinceDateTimeValidator>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
        {
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

            app.UseRouting();

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        protected IConfigurationRoot BuildConfiguration(IWebHostEnvironment hostingEnvironment)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true, true);

            builder.AddEnvironmentVariables();

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
        }
    }
}
