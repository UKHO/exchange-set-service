using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class ModelStructureCheck
    {
        public static async Task CheckModelStructureForSuccessResponse(this HttpResponseMessage apiresponse)
        {
            var apiresponsedata = await apiresponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiresponsedata.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Check ExchangeSetUrlExpiryDateTime is null
            Assert.IsNull(apiresponsedata.ExchangeSetUrlExpiryDateTime, $"Response body returns valid datetime {apiresponsedata.ExchangeSetUrlExpiryDateTime}, instead of null.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.RequestedProductCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.RequestedProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.ExchangeSetCellCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.ExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiresponsedata.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Responsebody returns other datatype, instead of expected Int");
            Assert.IsTrue(apiresponsedata.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

            //Check RequestedProductsNotInExchangeSet is Not null
            Assert.IsNotNull(apiresponsedata.RequestedProductsNotInExchangeSet, "Response body returns null for RequestedProductsNotInExchangeSet, instead of Not null");

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
            Guid guidID;
            bool hasGUID = Guid.TryParse(batchID, out guidID);

            //Verify the exchangeSetBatchStatusUri Batch ID
            Assert.IsTrue(hasGUID, $"Exchange set returned batch status URI {apiresponsedata.Links.ExchangeSetBatchStatusUri.Href}, with invalid BatchID");


            //Check ExchangeSetFileUri a valid Uri
            Assert.IsTrue(Uri.IsWellFormedUriString(apiresponsedata.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, Its not valid uri");


            string[] ExchangeSetFileUri = apiresponsedata.Links.ExchangeSetFileUri.Href.Split('/');

            //Verify the ExchangeSetFileUri format for batch
            Assert.AreEqual("batch", ExchangeSetFileUri[3], $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, which is wrong format.");
            //Verify the ExchangeSetFileUri format for files
            Assert.AreEqual("files", ExchangeSetFileUri[5], $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, which is wrong format.");

            var fileBatchId = ExchangeSetFileUri[4];
            bool hasGuid = Guid.TryParse(fileBatchId, out guidID);

            //Verify the ExchangeSetFileUri format for BatchID
            Assert.IsTrue(hasGuid, $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, with invalid BatchID");
            //Verify the File format for ExchangeSetFileUri
            Assert.AreEqual("V01X01.zip", ExchangeSetFileUri[6], $"Exchange set returned File URI {apiresponsedata.Links.ExchangeSetFileUri.Href}, which is wrong format.");

            // verify both batch ID of ExchangeSetBatchStatusUri and ExchangeSetFileUri are the same
            Assert.AreEqual(hasGUID, hasGuid, $"The Batch ID of ExchangeSetBatchStatusUri and ExchangeSetFileUri are not the same.");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiresponsedata.ExchangeSetUrlExpiryDateTime, $"Response body returns null, instead of valid Exchange Set Url ExpiryDateTime.");
        }
    }
}
