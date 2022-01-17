using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

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
    }
}
