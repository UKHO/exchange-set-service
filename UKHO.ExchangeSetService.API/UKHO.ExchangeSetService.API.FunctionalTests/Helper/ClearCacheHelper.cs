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
            GetUrl fileLink1 = new()
            {
                Href = @"/batch/35604ae5-7dc2-44cd-a819-01d4b1081979/files/DE290001.013"
            };
            GetUrl fileLink2 = new()
            {
                Href = @"/batch/35604ae5-7dc2-44cd-a819-01d4b1081979/files/DE290001.014"
            };
            RefLink link1 = new()
            {
                Get = fileLink1,
            };
            RefLink link2 = new()
            {
                Get = fileLink2,
            };
            LinksNew links = new()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                GetUrl = linkGet
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

                Files = new List<CacheFile>{new(){Filename = "DE290001.013", FileSize = 874,
                                                    MimeType = "text/plain", Hash = "rijY0LV7Ebpirybo4RYAiQ==",
                                                    Attributes = new List<Attribute>{},
                                                    Links = link1 },
                                            new(){Filename = "DE290001.014", FileSize = 1520, 
                                                 MimeType = "application/s63", Hash = "mp25B4rDzWfCyPjqI2f+5Q==",
                                                 Attributes = new List<Attribute>{new(){Key = "s57-CRC", Value = "CC362FA5" } },
                                                 Links = link2 }},

                BatchId = Guid.NewGuid().ToString(),
                BatchPublishedDate = DateTime.UtcNow
            };
        }

        public EnterpriseEventCacheDataRequest GetCacheRequestDataForReadMeFile(string businessUnit)
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
                Href = @"/batch/a07537ff-ffa2-4565-8f0e-96e61e70a9fc/files/README.TXT"
            };

            RefLink link = new()
            {
                Get = fileLink,
            };

            LinksNew links = new()
            {
                BatchDetails = linkBatchDetails,
                BatchStatus = linkBatchStatus,
                GetUrl = linkGet
            };

            return new EnterpriseEventCacheDataRequest
            {
                Links = links,
                BusinessUnit = businessUnit,
                Attributes = new List<Attribute> { new() { Key = "Product Type", Value = "AVCS" } },

                Files = new List<CacheFile>{new(){Filename = "README.TXT", FileSize = 44788,
                        MimeType = "text/plain", Hash = "SuWvMzKMj+fkeEFzWf7nlw==",
                        Attributes = new List<Attribute>{},
                        Links = link },
                },

                BatchId = Guid.NewGuid().ToString(),
                BatchPublishedDate = DateTime.UtcNow
            };
        }
    }
}
