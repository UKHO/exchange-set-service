using FakeItEasy;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Controllers;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.UnitTests.Controllers
{
    [TestFixture]
    public class ProductDataControllerTests
    {
        private ProductDataController controller;
        private IHttpContextAccessor fakeHttpContextAccessor;
        private IProductDataService fakeProductDataService;
        private ILogger<ProductDataController> fakeLogger;

        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeProductDataService = A.Fake<IProductDataService>();
            fakeLogger = A.Fake<ILogger<ProductDataController>>();

            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());

            controller = new ProductDataController(fakeHttpContextAccessor, fakeLogger, fakeProductDataService);
        }

        #region ProductVersions
        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("ProductName", "ProductName cannot be blank or null.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(
                new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } }, "");
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("ProductName cannot be blank or null.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenPostProductDataByProductVersionsReturnsBadRequest()

        {
            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (BadRequestObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>(), "");
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenPostProductDataByProductVersionsReturnsOkResponse()
        {
            var response = new ExchangeSetResponse();
            response.Links = new Links();
            response.Links.ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            response.Links.ExchangeSetFileUri = new LinkSetFileUri()
            {
                Href = "http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip"
            };
            response.ExchangeSetUrlExpiryDateTime = DateTime.Now;
            response.RequestedProductCount = 22;
            response.ExchangeSetCellCount = 15;
            response.RequestedProductsAlreadyUpToDateCount = 5;
            response.RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>() {
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123456",
                    Reason = "productWithdrawn"
                },
                new RequestedProductsNotInExchangeSet()
                {
                    ProductName = "GB123789",
                    Reason = "invalidProduct"
                }
            };
            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure>()));

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductVersions(A<ProductDataProductVersionsRequest>.Ignored))
                 .Returns(response);

            var result = (OkObjectResult)await controller.PostProductDataByProductVersions(new List<ProductVersionRequest>() 
                            { new ProductVersionRequest() { ProductName = "demo" } }, "");
            Assert.AreSame(response, result.Value);
        }
        #endregion
    }
}
