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
        private ILogger<ProductDataController> fakeLogger;
        private IProductDataService fakeproductDataService;

        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeproductDataService = A.Fake<IProductDataService>();
            fakeLogger = A.Fake<ILogger<ProductDataController>>();

            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());
            controller = new ProductDataController(fakeHttpContextAccessor, fakeLogger ,fakeproductDataService);
        }

        #region GetExchangeSetResponse
        private ExchangeSetResponse GetExchangeSetResponse()
        {
            LinkSetBatchStatusUri linkSetBatchStatusUri = new LinkSetBatchStatusUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272"
            };
            LinkSetFileUri linkSetFileUri = new LinkSetFileUri()
            {
                Href = @"http://fss.ukho.gov.uk/batch/7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272/files/exchangeset123.zip",
            };
            Links links = new Links()
            {
                ExchangeSetBatchStatusUri = linkSetBatchStatusUri,
                ExchangeSetFileUri = linkSetFileUri
            };
            List<RequestedProductsNotInExchangeSet> lstRequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>()
            {
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
            ExchangeSetResponse exchangeSetResponse = new ExchangeSetResponse()
            {
                Links = links,
                ExchangeSetUrlExpiryDateTime = Convert.ToDateTime("2021-02-17T16:19:32.269Z").ToUniversalTime(),
                RequestedProductCount = 22,
                ExchangeSetCellCount = 15,
                RequestedProductsAlreadyUpToDateCount = 5,
                RequestedProductsNotInExchangeSet = lstRequestedProductsNotInExchangeSet
            };
            return exchangeSetResponse;
        }

        #endregion GetExchangeSetResponse

        #region ProductDataSinceDateTime

        [Test]
        public async Task WhenEmptySinceDateTimeInRequest_ThenGetProductDataSinceDateTimeReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("SinceDateTime", "Query parameter 'SinceDateTime' is required.");

            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeproductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (BadRequestObjectResult)await controller.GetProductDataSinceDateTime(null, "https://www.abc.com");
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Query parameter 'SinceDateTime' is required.", errors.Errors.Single().Description);
        }

        [Test]
        public async Task WhenValidRequest_ThenGetProductDataSinceDateTimeReturnSuccess()
        {
            var exchangeSetResponse = GetExchangeSetResponse();
            A.CallTo(() => fakeproductDataService.ValidateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeproductDataService.CreateProductDataSinceDateTime(A<ProductDataSinceDateTimeRequest>.Ignored))
                .Returns(exchangeSetResponse);

            var result = (OkObjectResult)await controller.GetProductDataSinceDateTime("Wed, 21 Oct 2015 07:28:00 GMT", "https://www.abc.com");

            Assert.AreSame(exchangeSetResponse, result.Value);
        }
        
        #endregion ProductDataSinceDateTime
    }
}
