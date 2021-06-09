using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.API.Filters
{
    [ExcludeFromCodeCoverage]   //Used in Startup.cs
    public static class LoggingMiddleware
    {
        private const string RedactedValue = "********";
        private static readonly string[] HeadersToRedact = { "userpass", "token" };
        private const int maxBodyCharSize = 1000;
        private const int truncatedBodyCharSize = 987;

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

        public static IApplicationBuilder UseLogAllRequestsAndResponses(this IApplicationBuilder appBuilder, ILoggerFactory loggerFactory)
        {
            return appBuilder.Use(async (context, func) =>
            {
                var logger = loggerFactory
                    .CreateLogger(typeof(LoggingMiddleware).FullName);

                await LogRequestAndResponse(context, func, logger);
            });
        }

        public static async Task LogRequestAndResponse(HttpContext context, Func<Task> func, ILogger logger)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            using (var requestBodyStream = new MemoryStream())
            {
                using (var responseBody = new MemoryStream())
                {
                    var originalRequestBody = context.Request.Body;
                    await originalRequestBody.CopyToAsync(requestBodyStream);
                    context.Request.Body = requestBodyStream;

                    var requestBodyText = await ReadAndResetStream(requestBodyStream);
                    var url = context.Request.GetDisplayUrl();
                    var requestHeaders = RedactHeaders(context.Request.Headers);
                    var ipAddress = context.Request.HttpContext.Connection.RemoteIpAddress;

                    var originalResponseBody = context.Response.Body;

                    context.Response.Body = responseBody;
                    try
                    {
                        await func();
                    }
                    finally
                    {
                        context.Request.Body = originalRequestBody;
                        context.Response.Body = originalResponseBody;

                        var responseHeaders = RedactHeaders(context.Response.Headers);

                        responseBody.Seek(0, SeekOrigin.Begin);
                        if (responseBody.Length > 0)
                            await responseBody.CopyToAsync(originalResponseBody);

                        var bodyAsString = await ReadResponseBodyAsString(context, responseBody, logger);
                        stopwatch.Stop();
                        logger.LogInformation(EventIds.LogRequest.ToEventId(),
                                              "Request Method: {requestMethod}, Request Url: {url}, Request IP: {ipAddress}, Request Header:{requestHeaders}, Request Body: {requestBodyText}, Response Code: {responseCode}, Response Content Length: {responseContentLength}, Response Content Type: {responseContentType}, Response Headers:{responseHeaders}, Response Body: {responseBody}, Processing Time: {processingDuration}",
                                              context.Request.Method,
                                              url,
                                              ipAddress,
                                              requestHeaders,
                                              requestBodyText,
                                              context.Response.StatusCode,
                                              responseBody.Length,
                                              context.Response.ContentType,
                                              responseHeaders,
                                              bodyAsString,
                                              stopwatch.Elapsed);
                    }
                }
            }
        }

        private static async Task<string> ReadResponseBodyAsString(HttpContext context, Stream responseBody, ILogger logger)
        {
            if (context.Response.ContentLength == 0)
                return null;
            if (context.Response.ContentType?.IndexOf("json", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                return "Redacted as its not JSON.";
            }

            var bodyAsString = await ReadAndResetStream(responseBody);
            if (!string.IsNullOrEmpty(bodyAsString))
            {
                foreach (var propertyToRedact in HeadersToRedact)
                {
                    if (bodyAsString.Contains(propertyToRedact))
                    {
                        bodyAsString = RedactBody(propertyToRedact, bodyAsString, logger);
                    }
                }

                if (bodyAsString.Length > maxBodyCharSize)
                {
                    bodyAsString = bodyAsString.Remove(truncatedBodyCharSize) + "... Truncated";
                }
            }

            return bodyAsString;
        }

        private static string RedactBody(string propertyNameToRedact, string bodyAsString, ILogger logger)
        {
            try
            {
                var jobj = JObject.Parse(bodyAsString);
                RedactJObject(propertyNameToRedact, jobj);

                return jobj.ToString(Formatting.None);
            }
            catch (JsonReaderException e)
            {
                logger.LogWarning(EventIds.ErrorRedactingResponseBody.ToEventId(), e, "Error Redacting Response Body for property {propertyNameToRedact}", propertyNameToRedact);
                return bodyAsString;
            }
        }

        private static void RedactJObject(string propertyNameToRedact, JObject jobj)
        {
            foreach (var property in jobj.Descendants().OfType<JProperty>())
            {
                if (property.Name == propertyNameToRedact)
                    property.Value = RedactedValue;
            }
        }

        private static Dictionary<string, string> RedactHeaders(IHeaderDictionary headerDictionary)
        {
            return headerDictionary.Where(
                                          h => !h.Key.Equals("Authorization", StringComparison.InvariantCultureIgnoreCase)
                                               && !h.Key.Equals("Ocp-Apim-Subscription-Key", StringComparison.InvariantCultureIgnoreCase)
                                               && !h.Key.Equals("X-ARR-ClientCert", StringComparison.InvariantCultureIgnoreCase)
                                               && !h.Key.Equals("MS-ASPNETCORE-CLIENTCERT", StringComparison.InvariantCultureIgnoreCase)
                                         )
                .ToDictionary(h => h.Key, h => HeadersToRedact.Any(r => r.Equals(h.Key, StringComparison.InvariantCultureIgnoreCase)) ? RedactedValue : string.Join(", ", (object[])h.Value));
        }

        private static async Task<string> ReadAndResetStream(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            try
            {
                return await new StreamReader(stream).ReadToEndAsync();
            }
            finally
            {
                stream.Seek(0, SeekOrigin.Begin);
            }
        }        
    }
}
