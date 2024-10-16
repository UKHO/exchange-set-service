using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace UKHO.ExchangeSetService.API.Filters
{
    [ExcludeFromCodeCoverage] //Used in Startup.cs
    public class PermissionsPolicyHeaderMiddleware
    {
        private readonly RequestDelegate _next;

        public PermissionsPolicyHeaderMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext)
        {
            httpContext.Response.Headers.Append("Permissions-Policy", "camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), usb=()");
            return _next(httpContext);
        }
    }

    public static class PermissionsPolicyHeaderMiddlewareExtensions
    {
        /// <summary>
        /// Adds a middleware to the ASP.NET Core pipeline that sets the Permissions-Policy header.
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UsePermissionsPolicyHeader(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PermissionsPolicyHeaderMiddleware>();
        }
    }
}
