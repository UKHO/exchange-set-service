using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace UKHO.ExchangeSetService.API.Filters
{
    public static class LoggingMiddleware
    {
        [ExcludeFromCodeCoverage]   //Used in Startup.cs
        public static IApplicationBuilder UseErrorLogging(this IApplicationBuilder appBuilder, ILoggerFactory loggerFactory)
        {
            return appBuilder.Use(async (context, func) =>
            {
                try
                {
                    await func();
                }
                catch (Exception e)
                {
                    loggerFactory
                        .CreateLogger(context.Request.Path)
                        .LogError(e, "{Exception}", e);
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
            });
        }
    }
}
