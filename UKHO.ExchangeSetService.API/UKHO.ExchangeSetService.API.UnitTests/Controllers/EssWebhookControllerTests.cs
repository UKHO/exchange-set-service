using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;
using UKHO.ExchangeSetService.Common.Models.Request;
using Attribute = UKHO.ExchangeSetService.Common.Models.Request.Attribute;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class EssWebhookControllerTests
    {
        private EssWebhookController fakeWebHookController;
        private IEssWebhookService fakeEssWebhookService;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private ILogger<EssWebhookController> fakeLogger;
        private IAzureAdB2CHelper fakeAzureAdB2CHelper;

        [SetUp]
        public void Setup()
        {
            fakeEssWebhookService = A.Fake<IEssWebhookService>();
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeLogger = A.Fake<ILogger<EssWebhookController>>();
            fakeAzureAdB2CHelper = A.Fake<IAzureAdB2CHelper>();
            fakeWebHookController = new EssWebhookController(fakeHttpContextAccessor, fakeLogger, fakeEssWebhookService, fakeAzureAdB2CHelper);
        }

        [Test]
        public void WhenValidHeaderRequestedInNewFilesPublishedOptions_ThenReturnsOkResponse()
        {
            var responseHeaders = new HeaderDictionary();
            var httpContext = A.Fake<HttpContext>();

            A.CallTo(() => httpContext.Response.Headers).Returns(responseHeaders);
            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(httpContext);
            A.CallTo(() => httpContext.Request.Headers["WebHook-Request-Origin"]).Returns(new[] { "test.com" });

            var result = (OkObjectResult)fakeWebHookController.NewFilesPublishedOptions();

            Assert.That(result.StatusCode, Is.EqualTo(200));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.NewFilesPublishedWebhookOptionsCallStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Started processing the Options request for the New Files Published event webhook for WebHook-Request-Origin:{webhookRequestOrigin}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.NewFilesPublishedWebhookOptionsCallCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Completed processing the Options request for the New Files Published event webhook for WebHook-Request-Origin:{webhookRequestOrigin}").MustHaveHappenedOnceExactly();

            Assert.That(responseHeaders, Has.Count.EqualTo(2));
            Assert.Multiple(() =>
            {
                Assert.That(responseHeaders["WebHook-Allowed-Rate"].ToString(), Is.EqualTo("*"));
                Assert.That(responseHeaders["WebHook-Allowed-Origin"].ToString(), Is.EqualTo("test.com"));
            });
        }

        [Test]
        public async Task WhenB2CUserRequestedNewFilesPublished_ThenAuthorizeUser()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData("ADDS"));

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);

            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);

            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeEssWebhookService.InvalidateAndInsertCacheDataAsync(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            Assert.That(result.StatusCode, Is.EqualTo(200));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event started for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSB2CUserValidationEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Event was triggered with invalid Azure AD token from Enterprise event for ESS Invalidate and Insert Cache Event for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event completed as Azure AD Authentication failed with OK response and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenInvalidDataRequestedInNewFilesPublished_ThenValidateNulldata()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetInvalidCacheRequestData());

            var validationMessage = new ValidationFailure("PostESSWebhook", "BadRequest")
            {
                ErrorCode = HttpStatusCode.OK.ToString()
            };

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);

            A.CallTo(() => fakeEssWebhookService.InvalidateAndInsertCacheDataAsync(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            Assert.That(result.StatusCode, Is.EqualTo(200));

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event started for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Enterprise Event data deserialized in ESS and Data:{data} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataValidationEvent.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Required attributes missing in event data from Enterprise event for ESS Invalidate and Insert Cache Event for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event completed for ProductName:{productName} as required data was missing in payload with OK response and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidS63DataRequestedInNewFilesPublished_ThenInvalidateCachedS63DataFromStorage()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData("ADDS"));

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeEssWebhookService.InvalidateAndInsertCacheDataAsync(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored));

            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);

            result.StatusCode.Should().Be(200);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event started for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Enterprise Event data deserialized in ESS and Data:{data} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event completed for ProductName:{productName} of BusinessUnit:{businessUnit} with OK response and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenValidS57DataRequestedInNewFilesPublished_ThenInvalidateCachedS57DataFromStorage()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData("ADDS-S57"));

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeEssWebhookService.InvalidateAndInsertCacheDataAsync(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored));

            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);

            result.StatusCode.Should().Be(200);

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event started for _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventStart.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Enterprise Event data deserialized in ESS and Data:{data} and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call => call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ESSInvalidateAndInsertCacheDataEventCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS Invalidate and Insert Cache Data Event completed for ProductName:{productName} of BusinessUnit:{businessUnit} with OK response and _X-Correlation-ID:{correlationId}").MustHaveHappenedOnceExactly();
        }

        private EnterpriseEventCacheDataRequest GetCacheRequestData(string businessUnit)
        {
            BatchDetails linkBatchDetails = new BatchDetails()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272"
            };
            BatchStatus linkBatchStatus = new BatchStatus()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/status"
            };
            Get linkGet = new Get()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip",
            };
            CacheLinks links = new CacheLinks()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                Get = linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = businessUnit,
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        private EnterpriseEventCacheDataRequest GetInvalidCacheRequestData()
        {
            BatchDetails linkBatchDetails = new BatchDetails()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272"
            };
            BatchStatus linkBatchStatus = new BatchStatus()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/status"
            };
            Get linkGet = new Get()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip",
            };
            CacheLinks links = new CacheLinks()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                Get = linkGet
            };
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = "",
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "SEC" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }
    }
}
