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
        public const string errorMessage = "Either body is null or malformed";
      
        [SetUp]
        public void Setup()
        {
            fakeHttpContextAccessor = A.Fake<IHttpContextAccessor>();
            fakeProductDataService = A.Fake<IProductDataService>();
            fakeLogger = A.Fake<ILogger<ProductDataController>>(); 
            A.CallTo(() => fakeHttpContextAccessor.HttpContext).Returns(new DefaultHttpContext());
            controller = new ProductDataController(fakeHttpContextAccessor, fakeLogger, fakeProductDataService);
        }

        #region PostProductIdentifiers

        [Test]
        public async Task WhenInvalidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("ProductIdentifiers", "Product Identifiers cannot be null or empty.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            string[] productIdentifiers = new string[] { "", "GB160060", "AU334550" };
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Product Identifiers cannot be null or empty.", errors.Errors.Single().Description);

        }
        
        [Test]
        public async Task WhenNullProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(null, null);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

        
        [Test]
        public async Task WhenEmptyProductIdentifiersRequest_ThenPostProductIdentifiersReturnsBadRequest()
        {
            var validationMessage = new ValidationFailure("RequestBody", "Either body is null or malformed.");
            validationMessage.ErrorCode = HttpStatusCode.BadRequest.ToString();

            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                            .Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            string[] productIdentifiers = new string[] {};
            string callbackUri = string.Empty;

            var result = (BadRequestObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            var errors = (ErrorDescription)result.Value;
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual("Either body is null or malformed.", errors.Errors.Single().Description);
        }

      
        [Test]
        public async Task WhenValidProductIdentifiersRequest_ThenPostProductIdentifiersReturnsOkObjectResultCreated()
        {
            A.CallTo(() => fakeProductDataService.ValidateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(new ValidationResult(new List<ValidationFailure>()));            

            string[] productIdentifiers = new string[] {"GB123456", "GB160060","AU334550" };
            string callbackUri = string.Empty;

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

            A.CallTo(() => fakeProductDataService.CreateProductDataByProductIdentifiers(A<ProductIdentifierRequest>.Ignored))
                 .Returns(exchangeSetResponse);
            var result = (OkObjectResult)await controller.PostProductIdentifiers(productIdentifiers, callbackUri);
            
            Assert.AreEqual(exchangeSetResponse.ExchangeSetCellCount,
                ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).ExchangeSetCellCount);

            Assert.AreEqual(exchangeSetResponse.RequestedProductCount,
                ((UKHO.ExchangeSetService.Common.Models.Response.ExchangeSetResponse)result.Value).RequestedProductCount);

        }
        #endregion PostProductIdentifiers

    }
}
