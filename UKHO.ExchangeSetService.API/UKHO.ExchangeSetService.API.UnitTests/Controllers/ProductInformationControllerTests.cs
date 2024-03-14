using FakeItEasy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Linq;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ProductInformationControllerTests
    {
        private ProductInformationController controller;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private ILogger<ProductInformationController> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeLogger = A.Fake<ILogger<ProductInformationController>>();
            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());

            controller = new ProductInformationController(fakeHttpContextAccessor, fakeLogger);
        }

        #region GetProductInformationbySinceDateTime

        [Test]
        public void GetProductDataSinceDateTimeShouldReturnSuccess()
        {
            var result = (StatusCodeResult)controller.GetProductInformationSinceDateTime("Fri, 01 Feb 2024 09:00:00 GMT");

            Assert.AreEqual(StatusCodes.Status200OK, result.StatusCode);
        }

        [Test]
        public void WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeShouldReturnBadRequest()
        {
            var result = (BadRequestObjectResult)controller.GetProductInformationSinceDateTime(null);

            var errors = (ErrorDescription)result.Value;

            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("sinceDateTime", errors.Errors.Single().Source);
            Assert.AreEqual("Query parameter 'sinceDateTime' is required.", errors.Errors.Single().Description);
        }

        #endregion GetProductInformationbySinceDateTime
    }
}
