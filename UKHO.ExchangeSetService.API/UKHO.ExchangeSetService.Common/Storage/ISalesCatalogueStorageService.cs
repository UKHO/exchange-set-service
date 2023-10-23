using Azure.Storage;

namespace UKHO.ExchangeSetService.Common.Storage
{
    public interface ISalesCatalogueStorageService
    {
        string GetStorageAccountConnectionString(string storageAccountName = null, string storageAccountKey = null);
        StorageSharedKeyCredential GetStorageSharedKeyCredentials();
    }
}
