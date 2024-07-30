using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using Attribute = UKHO.ExchangeSetService.API.FunctionalTests.Models.Attribute;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class ClearCacheHelper
    {
        public async Task<ITableEntity> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : ITableEntity
        {
            var retrieveOperation = TableOperation.Retrieve<TElement>(partitionKey, rowKey);
            return await ExecuteTableOperation(retrieveOperation, tableName, storageAccountConnectionString) as ITableEntity;
        }

        private async Task<CloudTable> GetAzureTable(string tableName, string storageAccountConnectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());
            var table = tableClient.GetTableReference(tableName);
            await table.CreateIfNotExistsAsync();
            return table;
        }

        private async Task<object> ExecuteTableOperation(TableOperation tableOperation, string tableName, string storageAccountConnectionString)
        {
            var table = await GetAzureTable(tableName, storageAccountConnectionString);
            var tableResult = await table.ExecuteAsync(tableOperation);
            return tableResult.Result;
        }

        public EnterpriseEventCacheDataRequest GetCacheRequestData(string businessUnit, string agency, string product, int editionNumber)
        {
            BatchDetails linkBatchDetails = new()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272"
            };
            BatchStatus linkBatchStatus = new()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/status"
            };
            GetUrl linkGet = new()
            {
                Href = @"http://tempuri.org.uk/batch/7b4cdb10-ddfd-4ed6-b2be-d1543d8b7272/files/exchangeset123.zip"
            };
            GetUrl fileLink = new()
            {
                Href = @"/batch/35604ae5-7dc2-44cd-a819-01d4b1081978/files/DE216000.013"
            };
           
            LinksNew links = new()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                GetUrl = linkGet
            };

            RefLink link = new()
            {
                Get = fileLink
            };
            
            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = businessUnit,
                Attributes = new List<Attribute> { new() { Key= "Agency", Value= agency } ,
                                                           new Attribute { Key= "CellName", Value= product },
                                                           new Attribute { Key= "EditionNumber", Value= editionNumber.ToString() } ,
                                                           new Attribute { Key= "UpdateNumber", Value= "0" },
                                                           new Attribute { Key= "ProductCode", Value= "AVCS" }},

                Files = new List<CacheFile>{new(){Filename = "DE216000.013", FileSize = 1520,
                                                    MimeType = "application/s63", Hash = "mp25B4rDzWfCyPjqI2f+5Q==",
                                                    Attributes = new List<Attribute>{new(){Key = "s57-CRC", Value = "CC362FA5" } },
                                                    Links = link }},

                BatchId = "d6cd4d37-4d89-470d-9a33-82b3d7f54b6e",
                BatchPublishedDate = DateTime.UtcNow
            };
        }
    }
}
