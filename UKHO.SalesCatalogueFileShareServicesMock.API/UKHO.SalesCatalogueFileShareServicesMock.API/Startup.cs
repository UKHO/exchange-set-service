using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UKHO.SalesCatalogueFileShareServicesMock.API.Common;
using UKHO.SalesCatalogueFileShareServicesMock.API.Filters;
using UKHO.SalesCatalogueFileShareServicesMock.API.Services;
using UKHO.SalesCatalogueFileShareServicesMock.API.Wiremock.StubSetup;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.Configuration;
using UKHO.SalesCatalogueFileShareServicesMock.API.WireMock.StubSetup;
using WireMock.Settings;

namespace UKHO.SalesCatalogueFileShareServicesMock.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHeaderPropagation(options =>
            {
                options.Headers.Add(CorrelationIdMiddleware.XCorrelationIdHeaderKey);
            });
            services.AddControllers(o => o.InputFormatters.Insert(0, new BinaryRequestBodyFormatter()));

            services.AddScoped<SalesCatalogueService>();
            services.AddScoped<FileShareService>();
            services.Configure<SalesCatalogueConfiguration>(Configuration.GetSection("SalesCatalogue"));
            services.Configure<FileShareServiceConfiguration>(Configuration.GetSection("FileShareService"));

            services.Configure<WireMockServerSettings>(Configuration.GetSection("WireMockServerSettings"));
            services.Configure<SalesCatalogueServiceConfiguration>(Configuration.GetSection("SalesCatalagoueServiceConfiguration"));
            services.AddSingleton<StubFactory>();
            services.AddHostedService<StubManagerHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseCorrelationIdMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
