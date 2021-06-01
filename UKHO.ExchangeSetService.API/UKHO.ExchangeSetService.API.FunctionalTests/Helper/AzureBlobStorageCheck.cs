using Azure.Storage.Blobs;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class AzureBlobStorageCheck
    {
        /// <summary>
        /// Checks if Azure storage container with File name exists or not
        /// </summary>
        /// <param name="storageConnectionString">Storage Connection String</param>
        /// <param name="apiResponse">Api Response</param>
        /// <returns></returns>
        public static async Task<bool> CheckIfFileNameExist(string storageConnectionString, HttpResponseMessage apiResponse)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            string[] exchangeSetBatchStatusUri = apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Split('/');
            var batchId = exchangeSetBatchStatusUri[exchangeSetBatchStatusUri.Length - 1];
            var fileName = $"{batchId}.json";

            BlobContainerClient container = new BlobContainerClient(storageConnectionString, fileName);
            return await container.ExistsAsync();
        }
    }
}
