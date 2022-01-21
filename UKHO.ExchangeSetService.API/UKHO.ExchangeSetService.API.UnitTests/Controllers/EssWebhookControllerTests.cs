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
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.AzureADB2C;

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
            fakeWebHookController.ControllerContext.HttpContext = new DefaultHttpContext();
            fakeHttpContextAccessor.HttpContext.Request.Headers.Add("WebHook-Request-Origin", "test.example.com");

            var result = (OkObjectResult)fakeWebHookController.NewFilesPublishedOptions();

            Assert.AreEqual(200, result.StatusCode);
        }

        [Test]
        public async Task WhenB2CUserRequestedNewFilesPublished_ThenAuthorize()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData());

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(true);
            
            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);
            
            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeEssWebhookService.DeleteSearchAndDownloadCacheData(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored)).MustNotHaveHappened();

            Assert.AreEqual(200, result.StatusCode);
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

            A.CallTo(() => fakeEssWebhookService.DeleteSearchAndDownloadCacheData(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
           
            Assert.AreEqual(200, result.StatusCode);
        }

        [Test]
        public async Task WhenValidDataRequestedInNewFilesPublished_ThenDeleteFromStorage()
        {
            var fakeCacheJson = JObject.Parse(@"{""Type"":""FilesPublished""}");
            fakeCacheJson["Source"] = "https://www.fakecacheorg.co.uk";
            fakeCacheJson["Id"] = "25d6c6c1-418b-40f9-bb76-f6dfc0f133bc";
            fakeCacheJson["Data"] = JObject.FromObject(GetCacheRequestData());

            A.CallTo(() => fakeAzureAdB2CHelper.IsAzureB2CUser(A<AzureAdB2C>.Ignored, A<string>.Ignored)).Returns(false);
            A.CallTo(() => fakeEssWebhookService.ValidateEventGridCacheDataRequest(A<EnterpriseEventCacheDataRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeEssWebhookService.DeleteSearchAndDownloadCacheData(A<EnterpriseEventCacheDataRequest>.Ignored, A<string>.Ignored));

            var result = (OkObjectResult)await fakeWebHookController.NewFilesPublished(fakeCacheJson);

            Assert.AreEqual(200, result.StatusCode);
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

