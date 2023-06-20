using Azure.Data.Tables;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class ClearCacheHelper
    {
        public async Task<ITableEntity> RetrieveFromTableStorageAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : class, ITableEntity
        {
            var tableClient = await GetAzureTable(tableName, storageAccountConnectionString);
            var operation = await tableClient.GetEntityAsync<TElement>(partitionKey, rowKey);
            return operation.Value;
        }

        private static async Task<TableClient> GetAzureTable(string tableName, string storageAccountConnectionString)
        {
            var serviceClient = new TableServiceClient(storageAccountConnectionString);
            var tableClient = serviceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }
    }
}
