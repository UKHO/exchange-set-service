using Microsoft.Azure.Cosmos.Table;
using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureTableStorageClient
    {
        Task<ITableEntity> RetrieveAsync<TElement>(string partitionKey, string rowKey, string tableName, string storageAccountConnectionString) where TElement : ITableEntity;
        Task<ITableEntity> InsertOrMergeAsync(ITableEntity entity, string tableName, string storageAccountConnectionString);
        Task<ITableEntity> DeleteAsync(ITableEntity entity, string tableName, string storageAccountConnectionString);
        Task<CloudTable> GetCloudTable(string tableName, string storageAccountConnectionString);
    }
}
