using NUnit.Framework;
using System;
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
        public static async Task CheckModelStructureForSuccessResponse(this HttpResponseMessage apiResponse)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiresponsedata.ExchangeSetUrlExpiryDateTime, $"Response body returns null, Instead of valid datetime {apiresponsedata.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.RequestedProductCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.RequestedProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.ExchangeSetCellCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.ExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");             

        }


        public static async Task CheckModelStructureNotModifiedResponse(this HttpResponseMessage apiresponse)
        {
            var apiresponsedata = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Verify ExchangeSetCellCount
            Assert.AreEqual(1, apiresponsedata.ExchangeSetCellCount, $"Exchange set returned ExchangeSetCellCount {apiresponsedata.ExchangeSetCellCount}, instead of expected ExchangeSetCellCount 1.");

            //Check RequestedProductsNotInExchangeSet is empty
            Assert.IsEmpty(apiresponsedata.RequestedProductsNotInExchangeSet, "Response body returns Not Empty for RequestedProductsNotInExchangeSet, instead of Empty");
        }

        public static async Task CheckFssBatchResponse(this HttpResponseMessage apiresponse)
        {
            var apiresponsedata = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check if ExchangeSetBatchStatusUri is a valid Uri
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            
            string[] exchangeSetBatchStatusUri = apiresponsedata.Links.ExchangeSetBatchStatusUri.Href.Split('/');

            //Verify the exchangeSetBatchStatusUri format for batch
            Assert.AreEqual("batch", exchangeSetBatchStatusUri[3], $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, which is wrong format.");

            
            var batchID = exchangeSetBatchStatusUri[exchangeSetBatchStatusUri.Length - 1];
            
            bool hasGUID = Guid.TryParse(batchID, out Guid guidIdBatch);

            //Verify the exchangeSetBatchStatusUri contains BatchId is a valid GUID
            Assert.IsTrue(hasGUID, $"Exchange set returned batch status URI contains BatchId {batchID} is not a valid GUID");


            //Check ExchangeSetFileUri a valid Uri
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");


            string[] ExchangeSetFileUri = apiresponsedata.Links.ExchangeSetFileUri.Href.Split('/');

            //Verify the ExchangeSetFileUri format for batch
            Assert.AreEqual("batch", ExchangeSetFileUri[3], $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, which is wrong format.");
            //Verify the ExchangeSetFileUri format for files
            Assert.AreEqual("files", ExchangeSetFileUri[5], $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, which is wrong format.");

            var fileBatchId = ExchangeSetFileUri[4];
            hasGUID = Guid.TryParse(fileBatchId, out Guid guidIdFile);

            //Verify the ExchangeSetFileUri format for BatchID
            Assert.IsTrue(hasGUID, $"Exchange set returned file URI contains BatchId {fileBatchId} is not a valid GUID");
            
            //Verify the File format for ExchangeSetFileUri
            Assert.AreEqual(Config.ExchangeSetFileName, ExchangeSetFileUri[6], $"Exchange set returned File URI contains file name  {ExchangeSetFileUri[6]}, instead of expected file name {Config.ExchangeSetFileName}.");

            // verify both batch ID of ExchangeSetBatchStatusUri and ExchangeSetFileUri are the same
            Assert.AreEqual(batchID, fileBatchId, $"The Batch ID of ExchangeSetBatchStatusUri {batchID} and ExchangeSetFileUri {fileBatchId} are not equal.");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiresponsedata.ExchangeSetUrlExpiryDateTime, $"Response body returns null, instead of valid Exchange Set Url ExpiryDateTime {apiresponsedata.ExchangeSetUrlExpiryDateTime}.");

            //Verify expiry datetime
            var expiryDateTime = DateTime.UtcNow.AddDays(1);

            Assert.True(apiresponsedata.ExchangeSetUrlExpiryDateTime <= new DateTime(expiryDateTime.Year, expiryDateTime.Month, expiryDateTime.Day, expiryDateTime.Hour, expiryDateTime.Minute, expiryDateTime.Second), $"Response body returned ExpiryDateTime {apiresponsedata.ExchangeSetUrlExpiryDateTime} , greater than the expected value.");
        }
    }
}
