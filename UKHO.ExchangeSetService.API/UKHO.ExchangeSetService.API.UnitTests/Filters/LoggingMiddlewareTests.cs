using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FakeItEasy;
using FakeItEasy.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Filters;
using UKHO.ExchangeSetService.Common.Logging;

namespace UKHO.ExchangeSetService.API.UnitTests.Filters
{
    [TestFixture]
    public class LoggingMiddlewareTests
    {
        private ILogger _fakeLogger;
        private ILoggerFactory _fakeLoggerFactory;

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger>();
            _fakeLoggerFactory = A.Fake<ILoggerFactory>();
        }

        [TearDown]
        public void Teardown()
        {
            _fakeLoggerFactory.Dispose();
        }

        private static DefaultHttpContext CreateHttpContext(string method = "POST", string requestBody = "", string path = "/api/test", string queryString = "")
        {
            var context = new DefaultHttpContext();

            context.Request.Method = method;
            context.Request.Scheme = "https";
            context.Request.Host = new HostString("localhost");
            context.Request.Path = path;
            context.Request.QueryString = new QueryString(queryString);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody));
            context.Response.Body = new MemoryStream();

            return context;
        }

        private static ApplicationBuilder CreateApplicationBuilder() => new(new ServiceCollection().BuildServiceProvider());

        private static ICompletedFakeObjectCall GetLogCall(ILogger logger, LogLevel logLevel, EventIds eventId) => Fake.GetCalls(logger).Single(call => call.Method.Name == "Log" && call.GetArgument<LogLevel>(0) == logLevel && call.GetArgument<EventId>(1) == eventId.ToEventId());

        private static Dictionary<string, object> GetLogState(ICompletedFakeObjectCall logCall) => logCall.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(x => x.Key, x => x.Value);

        private static async Task<string> ReadBodyAsync(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
            return await reader.ReadToEndAsync();
        }

        [Test]
        public async Task WhenUseErrorLoggingHandlesException_ThenItSetsInternalServerErrorAndLogsError()
        {
            var appBuilder = CreateApplicationBuilder();
            var context = CreateHttpContext(path: "/error");
            var expectedException = new InvalidOperationException("boom");

            A.CallTo(() => _fakeLoggerFactory.CreateLogger("/error")).Returns(_fakeLogger);

            appBuilder.UseErrorLogging(_fakeLoggerFactory);
            appBuilder.Run(_ => Task.FromException(expectedException));

            var pipeline = appBuilder.Build();
            await pipeline(context);

            Assert.That(context.Response.StatusCode, Is.EqualTo((int)HttpStatusCode.InternalServerError));

            var logCall = GetLogCall(_fakeLogger, LogLevel.Error, EventIds.UnhandledControllerException);
            var logState = GetLogState(logCall);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logState["Exception"], Is.EqualTo(expectedException));
                Assert.That(logState["{OriginalFormat}"], Is.EqualTo("Unhandled controller exception {Exception}"));
            }
        }

        [Test]
        public async Task WhenUseLogAllRequestsAndResponsesIsConfigured_ThenItCreatesLoggerForLoggingMiddleware()
        {
            var appBuilder = CreateApplicationBuilder();
            var context = CreateHttpContext();
            var categoryName = typeof(LoggingMiddleware).FullName;

            A.CallTo(() => _fakeLoggerFactory.CreateLogger(categoryName)).Returns(_fakeLogger);

            appBuilder.UseLogAllRequestsAndResponses(_fakeLoggerFactory);
            appBuilder.Run(async httpContext =>
            {
                const string responseBody = "{\"message\":\"ok\"}";
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(responseBody);

                await httpContext.Response.WriteAsync(responseBody);
            });

            var pipeline = appBuilder.Build();
            await pipeline(context);

            A.CallTo(() => _fakeLoggerFactory.CreateLogger(categoryName)).MustHaveHappenedOnceExactly();
            var logCall = GetLogCall(_fakeLogger, LogLevel.Information, EventIds.LogRequest);
            var logState = GetLogState(logCall);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logState["requestMethod"], Is.EqualTo("POST"));
                Assert.That(logState["url"], Is.EqualTo("https://localhost/api/test"));
                Assert.That(logState["responseCode"], Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(logState["responseContentType"], Is.EqualTo("application/json"));
                Assert.That(logState["responseBody"], Is.EqualTo("{\"message\":\"ok\"}"));
                Assert.That(logState["{OriginalFormat}"], Is.EqualTo("Request Method: {requestMethod}, Request Url: {url}, Request IP: {ipAddress}, Request Header:{requestHeaders}, Request Body: {requestBodyText}, Response Code: {responseCode}, Response Content Length: {responseContentLength}, Response Content Type: {responseContentType}, Response Headers:{responseHeaders}, Response Body: {responseBody}, Processing Time: {processingDuration}"));
            }
        }

        [Test]
        public async Task WhenLogRequestAndResponseIsCalled_ThenItLogsSanitizedAndRedactedValuesAndCopiesResponseBody()
        {
            var context = CreateHttpContext("PO\r\nST", "request\r\n-body", "/api/products", "?id=1");
            context.Request.Headers.Append("token", "secret-token");
            context.Request.Headers.Append("Authorization", "Bearer hidden");
            context.Request.Headers.Append("X-Custom", "value\r\n");
            context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

            const string responseJson = "{\"token\":\"secret\",\"message\":\"ok\"}";

            await LoggingMiddleware.LogRequestAndResponse(context, async () =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(responseJson);
                context.Response.Headers.Append("userpass", "should-be-redacted");
                context.Response.Headers.Append("MS-ASPNETCORE-CLIENTCERT", "hidden");
                context.Response.Headers.Append("X-Response", "value\r\n");

                await context.Response.WriteAsync(responseJson);
            }, _fakeLogger);

            Assert.That(await ReadBodyAsync(context.Response.Body), Is.EqualTo(responseJson));

            var logCall = GetLogCall(_fakeLogger, LogLevel.Information, EventIds.LogRequest);
            var logState = GetLogState(logCall);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(logState["requestMethod"], Is.EqualTo("POST"));
                Assert.That(logState["url"], Is.EqualTo("https://localhost/api/products?id=1"));
                Assert.That(logState["ipAddress"], Is.EqualTo(IPAddress.Parse("127.0.0.1")));
                Assert.That(logState["requestBodyText"], Is.EqualTo("request-body"));
                Assert.That(logState["responseCode"], Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(logState["responseContentType"], Is.EqualTo("application/json"));
                Assert.That(logState["responseBody"], Is.EqualTo("{\"token\":\"********\",\"message\":\"ok\"}"));
                Assert.That(logState["{OriginalFormat}"], Is.EqualTo("Request Method: {requestMethod}, Request Url: {url}, Request IP: {ipAddress}, Request Header:{requestHeaders}, Request Body: {requestBodyText}, Response Code: {responseCode}, Response Content Length: {responseContentLength}, Response Content Type: {responseContentType}, Response Headers:{responseHeaders}, Response Body: {responseBody}, Processing Time: {processingDuration}"));
            }

            var requestHeaders = (Dictionary<string, string>)logState["requestHeaders"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(requestHeaders["token"], Is.EqualTo("********"));
                Assert.That(requestHeaders["X-Custom"], Is.EqualTo("value"));
                Assert.That(requestHeaders.ContainsKey("Authorization"), Is.False);
            }

            var responseHeaders = (Dictionary<string, string>)logState["responseHeaders"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(responseHeaders["userpass"], Is.EqualTo("********"));
                Assert.That(responseHeaders["X-Response"], Is.EqualTo("value"));
                Assert.That(responseHeaders.ContainsKey("MS-ASPNETCORE-CLIENTCERT"), Is.False);
            }
        }

        [Test]
        public async Task WhenResponseContentTypeIsNotJson_ThenItLogsRedactedMessageForResponseBody()
        {
            var context = CreateHttpContext();

            await LoggingMiddleware.LogRequestAndResponse(context, async () =>
            {
                const string responseBody = "plain text body";
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "text/plain";
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(responseBody);

                await context.Response.WriteAsync(responseBody);
            }, _fakeLogger);

            var logCall = GetLogCall(_fakeLogger, LogLevel.Information, EventIds.LogRequest);
            var logState = GetLogState(logCall);

            Assert.That(logState["responseBody"], Is.EqualTo("Redacted as its not JSON."));
        }

        [Test]
        public async Task WhenResponseContainsInvalidJsonForRedaction_ThenItLogsWarningAndOriginalResponseBody()
        {
            var context = CreateHttpContext();
            const string invalidJsonBody = "token=abc";

            await LoggingMiddleware.LogRequestAndResponse(context, async () =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = Encoding.UTF8.GetByteCount(invalidJsonBody);

                await context.Response.WriteAsync(invalidJsonBody);
            }, _fakeLogger);

            var warningCall = GetLogCall(_fakeLogger, LogLevel.Warning, EventIds.ErrorRedactingResponseBody);
            var warningState = GetLogState(warningCall);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(warningState["propertyNameToRedact"], Is.EqualTo("token"));
                Assert.That(warningState["{OriginalFormat}"], Is.EqualTo("Error Redacting Response Body for property {propertyNameToRedact}"));
            }

            var infoCall = GetLogCall(_fakeLogger, LogLevel.Information, EventIds.LogRequest);
            var infoState = GetLogState(infoCall);

            Assert.That(infoState["responseBody"], Is.EqualTo(invalidJsonBody));
        }

        [Test]
        public async Task WhenResponseContentLengthIsZero_ThenItLogsNullResponseBody()
        {
            var context = CreateHttpContext();

            await LoggingMiddleware.LogRequestAndResponse(context, () =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength = 0;

                return Task.CompletedTask;
            }, _fakeLogger);

            var logCall = GetLogCall(_fakeLogger, LogLevel.Information, EventIds.LogRequest);
            var logState = GetLogState(logCall);

            Assert.That(logState["responseBody"], Is.Null);
        }
    }
}
