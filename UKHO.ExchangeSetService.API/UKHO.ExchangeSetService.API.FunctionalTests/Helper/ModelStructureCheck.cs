using NUnit.Framework;
using System;
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
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            if (shouldEncFileUriExist)
            {
                //Check ExchangeSetFileUri is Not null and it is a valid Uri
                Assert.IsNotNull(apiResponseData.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
                Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.IsNull(apiResponseData.Links.ExchangeSetFileUri, "Exchange Set File uri should be null");
            }

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiResponseData.ExchangeSetUrlExpiryDateTime, $"Response body returns null, Instead of valid datetime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

        }


        public static async Task CheckModelStructureNotModifiedResponse(this HttpResponseMessage apiresponse)
        {
            var apiResponseData = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Verify ExchangeSetCellCount
            Assert.AreEqual(1, apiResponseData.ExchangeSetCellCount, $"Exchange set returned ExchangeSetCellCount {apiResponseData.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount 1.");

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.AreEqual("duplicateProduct", apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason, $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'duplicateProduct'");
        }

        public static async Task CheckFssBatchResponse(this HttpResponseMessage apiresponse)
        {
            var apiResponseData = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check if ExchangeSetBatchStatusUri is a valid Uri
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");


            string[] exchangeSetBatchStatusUri = apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Split('/');

            //Verify the exchangeSetBatchStatusUri format for batch
            Assert.AreEqual("batch", exchangeSetBatchStatusUri[4], $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, which is wrong format.");


            var batchID = exchangeSetBatchStatusUri[exchangeSetBatchStatusUri.Length - 2];

            bool hasGUID = Guid.TryParse(batchID, out Guid guidIdBatch);

            //Verify the exchangeSetBatchStatusUri contains BatchId is a valid GUID
            Assert.IsTrue(hasGUID, $"Exchange set returned batch status URI contains BatchId {batchID} is not a valid GUID");


            //Check ExchangeSetFileUri a valid Uri
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");


            string[] ExchangeSetFileUri = apiResponseData.Links.ExchangeSetFileUri.Href.Split('/');

            //Verify the ExchangeSetFileUri format for batch
            Assert.AreEqual("batch", ExchangeSetFileUri[4], $"Exchange set returned File URI {apiResponseData.Links.ExchangeSetFileUri.Href}, which is wrong format.");
            //Verify the ExchangeSetFileUri format for files
            Assert.AreEqual("files", ExchangeSetFileUri[6], $"Exchange set returned File URI {apiResponseData.Links.ExchangeSetFileUri.Href}, which is wrong format.");

            var fileBatchId = ExchangeSetFileUri[5];
            hasGUID = Guid.TryParse(fileBatchId, out Guid guidIdFile);

            //Verify the ExchangeSetFileUri format for BatchID
            Assert.IsTrue(hasGUID, $"Exchange set returned file URI contains BatchId {fileBatchId} is not a valid GUID");

            //Verify the File format for ExchangeSetFileUri
            Assert.AreEqual(Config.ExchangeSetFileName, ExchangeSetFileUri[7], $"Exchange set returned File URI contains file name  {ExchangeSetFileUri[7]}, instead of expected file name {Config.ExchangeSetFileName}.");

            // verify both batch ID of ExchangeSetBatchStatusUri and ExchangeSetFileUri are the same
            Assert.AreEqual(batchID, fileBatchId, $"The Batch ID of ExchangeSetBatchStatusUri {batchID} and ExchangeSetFileUri {fileBatchId} are not equal.");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiResponseData.ExchangeSetUrlExpiryDateTime, $"Response body returns null, instead of valid Exchange Set Url ExpiryDateTime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Verify expiry datetime
            var expiryDateTime = DateTime.UtcNow.AddDays(1).AddMinutes(1);

            Assert.True(apiResponseData.ExchangeSetUrlExpiryDateTime <= new DateTime(expiryDateTime.Year, expiryDateTime.Month, expiryDateTime.Day, expiryDateTime.Hour, expiryDateTime.Minute, expiryDateTime.Second), $"Response body returned ExpiryDateTime {apiResponseData.ExchangeSetUrlExpiryDateTime} , greater than the expected value.");
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
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            if (shouldEncFileUriExist)
            {
                //Check ExchangeSetFileUri is Not null and it is a valid Uri
                Assert.IsNotNull(apiResponseData.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
                Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.IsNull(apiResponseData.Links.ExchangeSetFileUri, "Exchange Set File uri should be null");
            }

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiResponseData.ExchangeSetUrlExpiryDateTime, $"Response body returns null, Instead of valid datetime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");


            if (shouldAioFileUriExist)
            {
                //Check AIOExchangeSetFileUri is Not null and it is a valid Uri
                Assert.IsNotNull(apiResponseData.Links.AioExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
                Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.AioExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.AioExchangeSetFileUri.Href}, Its not valid uri");
            }
            else
            {
                Assert.IsNull(apiResponseData.Links.AioExchangeSetFileUri, "Exchange Set File uri should be null");
            }


            //Check data type of AIORequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedAioProductCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedAioProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of AIOExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.AioExchangeSetCellCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.AioExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of AIORequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedAioProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedAioProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

            
        }
    }
}
