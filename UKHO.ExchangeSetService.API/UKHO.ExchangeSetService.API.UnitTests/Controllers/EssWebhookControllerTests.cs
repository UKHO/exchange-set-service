using FakeItEasy;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Request;
using Attribute = UKHO.ExchangeSetService.Common.Models.Request.Attribute;
using System.Net;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{

    [TestFixture]
    public class EssWebhookControllerTests
    {
        private EssWebhookController fakeWebHookController;
        private IEssWebhookService fakeEssWebhookService;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private ILogger<EssWebhookController> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeEssWebhookService = A.Fake<IEssWebhookService>();
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeLogger = A.Fake<ILogger<EssWebhookController>>();
            fakeWebHookController = new EssWebhookController(fakeHttpContextAccessor, fakeLogger, fakeEssWebhookService);
        }

        private EnterpriseEventCacheDataRequest GetCacheRequestData()
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
                BusinessUnit = "ADDS",
                Attributes = new List<Attribute> { new Attribute { Key= "Agency", Value= "DE" } ,
                                                           new Attribute { Key= "CellName", Value= "DE416050" },
                                                           new Attribute { Key= "EditionNumber", Value= "2" } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},
                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        [Test]
        public void WhenValidHeaderRequestedInOptions_ThenReturnsOkResponse()
        {
            fakeWebHookController.ControllerContext.HttpContext = new DefaultHttpContext();
            fakeHttpContextAccessor.HttpContext.Request.Headers.Add("WebHook-Request-Origin", "test.example.com");

            var result = (OkObjectResult)fakeWebHookController.Options();

            Assert.AreEqual(200, result.StatusCode);
        }

        [Test]
        public async Task WhenNullDataRequestedInPostEssWebhook_ThenValidateNulldata()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");            
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";            
            fakeCacheJson["Data"] = JObject.FromObject(new { Data = string.Empty }); 

            var validationMessage = new ValidationFailure("PostESSWebhook", "BadRequest")
            {
                ErrorCode = HttpStatusCode.OK.ToString()
            };

            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (OkObjectResult)await fakeWebHookController.PostEssWebhook(fakeCacheJson);
            
            Assert.AreEqual(200, result.StatusCode);
        }

        [Test]
        public async Task WhenValidDataRequestedInPostEssWebhook_ThenDeleteFromStorage()
        {            
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData());

            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));
            var result = (OkObjectResult)await fakeWebHookController.PostEssWebhook(fakeCacheJson);

            Assert.AreEqual(200, result.StatusCode);            
        }
    }
}

