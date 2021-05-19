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
    }
}
