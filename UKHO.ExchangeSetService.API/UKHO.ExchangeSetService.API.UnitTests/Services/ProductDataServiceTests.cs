using FakeItEasy;
using FluentValidation.Results;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.Services;
using UKHO.ExchangeSetService.API.Validation;
using UKHO.ExchangeSetService.Common.Models.Request;
using UKHO.ExchangeSetService.Common.Models.Response;

namespace UKHO.ExchangeSetService.API.UnitTests.Services
{
    [TestFixture]
    public class ProductDataServiceTests
    {
        private IProductDataProductVersionsValidator fakeProductVersionValidator;
        private IProductDataService fakeProductDataService;

        [SetUp]
        public void Setup()
        {
            fakeProductVersionValidator = A.Fake<IProductDataProductVersionsValidator>();

            fakeProductDataService = new ProductDataService(fakeProductVersionValidator);
        }

        #region ProductVersions
        
        [Test]
        public async Task WhenInvalidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("ProductName", "ProductName cannot be blank or null.")}));

            var result = await fakeProductDataService.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest() 
                    { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = null } } });
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("ProductName cannot be blank or null.", result.Errors.Single().ErrorMessage);
        }
        
        [Test]
        public async Task WhenInvalidNullProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsBadrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>
                    {new ValidationFailure("RequestBody", "Either body is null or malformed.")}));

            var result = await fakeProductDataService.ValidateProductDataByProductVersions(null);
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual("Either body is null or malformed.", result.Errors.Single().ErrorMessage);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenValidateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await fakeProductDataService.ValidateProductDataByProductVersions(new ProductDataProductVersionsRequest()
                            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest() { ProductName = "Demo", EditionNumber = 5, UpdateNumber = 0 } } });
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task WhenValidProductVersionRequest_ThenCreateProductDataByProductVersionsReturnsOkrequest()
        {
            A.CallTo(() => fakeProductVersionValidator.Validate(A<ProductDataProductVersionsRequest>.Ignored))
                .Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = await fakeProductDataService.CreateProductDataByProductVersions(new ProductDataProductVersionsRequest()
            { ProductVersions = new List<ProductVersionRequest>() { new ProductVersionRequest { 
            ProductName = "GB123789", EditionNumber = 6, UpdateNumber = 3 } }, CallbackUri = "" });
            Assert.IsInstanceOf<ExchangeSetResponse>(result);
        }
        #endregion
    }
}
