using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using UKHO.ExchangeSetService.Common.Helpers;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class CommonHelperTest
    {

        #region SalesCatalogueResponse
        private SalesCatalogueResponse GetSalesCatalogueFileSizeResponse()
        {
            return new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    ProductCounts = new ProductCounts
                    {
                        RequestedProductCount = 6,
                        RequestedProductsAlreadyUpToDateCount = 8,
                        ReturnedProductCount = 2,
                        RequestedProductsNotReturned = new List<RequestedProductsNotReturned> {
                                new RequestedProductsNotReturned { ProductName = "GB123456", Reason = "productWithdrawn" },
                                new RequestedProductsNotReturned { ProductName = "GB123789", Reason = "invalidProduct" }
                            }
                    },
                    Products = new List<Products> {
                            new Products {
                                ProductName = "productName",
                                EditionNumber = 2,
                                UpdateNumbers = new List<int?> { 3, 4 },
                                Cancellation = new Cancellation {
                                    EditionNumber = 4,
                                    UpdateNumber = 6
                                },
                                FileSize = 500
                            }
                        }
                }
            };
        }
        #endregion SalesCatalogueResponse
        [Test]
        public void CheckMethodReturns_CorrectWeekNumer()
        {
            var week1 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/07"));
            var week26 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/07/01"));
            var week53 = CommonHelper.GetCurrentWeekNumber(Convert.ToDateTime("2021/01/01"));

            Assert.AreEqual(1, week1);
            Assert.AreEqual(26, week26);
            Assert.AreEqual(53, week53);
        }
        [Test]
        public void CheckConversionOfBytesToMegabytes()
        {
            var fileSize = CommonHelper.ConvertBytesToMegabytes((long)4194304);
            Assert.AreEqual(4, fileSize);
        }

        [Test]
        public void CheckGetFileSize()
        {
            SalesCatalogueResponse salesCatalogueResponse = GetSalesCatalogueFileSizeResponse();
            int fileSize = CommonHelper.GetFileSize(salesCatalogueResponse.ResponseBody);
            Assert.AreEqual(500, fileSize);
        }
    }
}
