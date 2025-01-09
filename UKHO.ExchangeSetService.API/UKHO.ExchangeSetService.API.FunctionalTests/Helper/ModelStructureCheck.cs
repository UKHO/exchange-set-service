using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class ModelStructureCheck
    {
        private static TestConfiguration Config { get; set; }
        static ModelStructureCheck()
        {
            Config = new TestConfiguration();
        }
        public static async Task CheckModelStructureForSuccessResponse(this HttpResponseMessage apiResponse, bool shouldEncFileUriExist = true)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, Is.Not.Null, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            if (shouldEncFileUriExist)
            {
                //Check ExchangeSetFileUri is Not null and it is a valid Uri
                Assert.That(apiResponseData.Links.ExchangeSetFileUri.Href, Is.Not.Null, "Response body returns null instead of valid links.");
                Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.That(apiResponseData.Links.ExchangeSetFileUri, Is.Null, "Exchange Set File uri should be null");
            }

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime, Is.Not.Null, $"Response body returns null, Instead of valid datetime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedProductCount >= 0, Is.True, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.ExchangeSetCellCount >= 0, Is.True, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0, Is.True, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

        }


        public static async Task CheckModelStructureNotModifiedResponse(this HttpResponseMessage apiresponse)
        {
            var apiResponseData = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, Is.Not.Null, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetFileUri.Href, Is.Not.Null, "Response body returns null instead of valid links.");
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Verify ExchangeSetCellCount
            Assert.That(apiResponseData.ExchangeSetCellCount, Is.EqualTo(1), $"Exchange set returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount 1.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet, Is.Not.Null, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, Is.EqualTo("duplicateProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'duplicateProduct'");
        }

        public static async Task CheckFssBatchResponse(this HttpResponseMessage apiresponse)
        {
            var apiResponseData = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check if ExchangeSetBatchStatusUri is a valid Uri
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");


            string[] exchangeSetBatchStatusUri = apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Split('/');

            //Verify the exchangeSetBatchStatusUri format for batch
            Assert.That(exchangeSetBatchStatusUri[4], Is.EqualTo("batch"), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, which is wrong format.");


            var batchID = exchangeSetBatchStatusUri[exchangeSetBatchStatusUri.Length - 2];

            bool hasGUID = Guid.TryParse(batchID, out Guid guidIdBatch);

            //Verify the exchangeSetBatchStatusUri contains BatchId is a valid GUID
            Assert.That(hasGUID, Is.True, $"Exchange set returned batch status URI contains BatchId {batchID} is not a valid GUID");


            //Check ExchangeSetFileUri a valid Uri
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");


            string[] ExchangeSetFileUri = apiResponseData.Links.ExchangeSetFileUri.Href.Split('/');

            //Verify the ExchangeSetFileUri format for batch
            Assert.That(ExchangeSetFileUri[4], Is.EqualTo("batch"), $"Exchange set returned File URI {apiResponseData.Links.ExchangeSetFileUri.Href}, which is wrong format.");
            //Verify the ExchangeSetFileUri format for files
            Assert.That(ExchangeSetFileUri[6], Is.EqualTo("files"), $"Exchange set returned File URI {apiResponseData.Links.ExchangeSetFileUri.Href}, which is wrong format.");

            var fileBatchId = ExchangeSetFileUri[5];
            hasGUID = Guid.TryParse(fileBatchId, out Guid guidIdFile);

            //Verify the ExchangeSetFileUri format for BatchID
            Assert.That(hasGUID, Is.True, $"Exchange set returned file URI contains BatchId {fileBatchId} is not a valid GUID");

            //Verify the File format for ExchangeSetFileUri
            Assert.That(ExchangeSetFileUri[7], Is.EqualTo(Config.ExchangeSetFileName), $"Exchange set returned File URI contains file name  {ExchangeSetFileUri[7]}, instead of expected file name {Config.ExchangeSetFileName}.");

            // verify both batch ID of ExchangeSetBatchStatusUri and ExchangeSetFileUri are the same
            Assert.That(fileBatchId, Is.EqualTo(batchID), $"The Batch ID of ExchangeSetBatchStatusUri {batchID} and ExchangeSetFileUri {fileBatchId} are not equal.");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime, Is.Not.Null, $"Response body returns null, instead of valid Exchange Set Url ExpiryDateTime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Verify expiry datetime
            var expiryDateTime = DateTime.UtcNow.AddDays(1).AddMinutes(1);

            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime <= new DateTime(expiryDateTime.Year, expiryDateTime.Month, expiryDateTime.Day, expiryDateTime.Hour, expiryDateTime.Minute, expiryDateTime.Second), Is.True, $"Response body returned ExpiryDateTime {apiResponseData.ExchangeSetUrlExpiryDateTime} , greater than the expected value.");
        }

        public static async Task<string> GetBatchId(this HttpResponseMessage apiResponse)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();
            var batchId = apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Split('/')[5];
            return batchId;

        }

        public static async Task CheckModelStructureForAioSuccessResponse(this HttpResponseMessage apiResponse, bool shouldEncFileUriExist = true, bool shouldAioFileUriExist = true)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, Is.Not.Null, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            if (shouldEncFileUriExist)
            {
                //Check ExchangeSetFileUri is Not null and it is a valid Uri
                Assert.That(apiResponseData.Links.ExchangeSetFileUri.Href, Is.Not.Null, "Response body returns null instead of valid links.");
                Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.That(apiResponseData.Links.ExchangeSetFileUri, Is.Null, "Exchange Set File uri should be null");
            }

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime, Is.Not.Null, $"Response body returns null, Instead of valid datetime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedProductCount >= 0, Is.True, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.ExchangeSetCellCount >= 0, Is.True, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0, Is.True, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");


            if (shouldAioFileUriExist)
            {
                //Check AIOExchangeSetFileUri is Not null and it is a valid Uri
                Assert.That(apiResponseData.Links.AioExchangeSetFileUri.Href, Is.Not.Null, "Response body returns null instead of valid links.");
                Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.AioExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), Is.True, $"Exchange set returned file URI {apiResponseData.Links.AioExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.That(apiResponseData.Links.AioExchangeSetFileUri, Is.Null, "Exchange Set File uri should be null");
            }


            //Check data type of AIORequestedProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedAioProductCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedAioProductCount >= 0, Is.True, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of AIOExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.AioExchangeSetCellCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.AioExchangeSetCellCount >= 0, Is.True, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of AIORequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), Is.True, "Responsebody returns other datatype, instead of expected Int");
            Assert.That(apiResponseData.RequestedAioProductsAlreadyUpToDateCount >= 0, Is.True, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

        }

        /// <summary>
        /// This method is used to verify the ESS S100 API response body structure.
        /// </summary>
        /// <param name="apiResponse"></param>
        /// <param name="requestedProductCount"></param>
        /// <param name="exchangeSetProductCount"></param>
        /// <param name="requestedProductsAlreadyUpToDateCount"></param>
        /// <param name="requestedProductsNotInExchangeSet"></param>
        /// <returns></returns>
        public static async Task VerifyEssS100ApiResponseBodyDetails(this HttpResponseMessage apiResponse, int requestedProductCount, int exchangeSetProductCount, int requestedProductsAlreadyUpToDateCount, Dictionary<string, string> requestedProductsNotInExchangeSet = null)
        {
            var responseBody = JsonConvert.DeserializeObject<ExchangeSetBatch>(await apiResponse.Content.ReadAsStringAsync());
            Assert.That(responseBody.RequestedProductCount == requestedProductCount, $"RequestedProductCount was expected {requestedProductCount} but found " + responseBody.RequestedProductCount);
            Assert.That(responseBody.ExchangeSetProductCount == exchangeSetProductCount, $"ExchangeSetProductCount was expected {exchangeSetProductCount} but found " + responseBody.ExchangeSetProductCount);
            Assert.That(responseBody.RequestedProductsAlreadyUpToDateCount == requestedProductsAlreadyUpToDateCount, $"RequestedProductsAlreadyUpToDateCount was expected {requestedProductsAlreadyUpToDateCount} but found " + responseBody.RequestedProductsAlreadyUpToDateCount);

            foreach (var product in responseBody.RequestedProductsNotInExchangeSet)
            {
                Assert.That(requestedProductsNotInExchangeSet.ContainsKey(product.ProductName),
                    $"Product Name {product.ProductName} not found in requested products not in exchange set.");

                var expectedValue = requestedProductsNotInExchangeSet[product.ProductName];
                Assert.That(product.Reason, Is.EqualTo(expectedValue), $"For Product Name {product.ProductName}, expected value was {expectedValue} but found {product.Reason}.");
            }
        }
    }
}
