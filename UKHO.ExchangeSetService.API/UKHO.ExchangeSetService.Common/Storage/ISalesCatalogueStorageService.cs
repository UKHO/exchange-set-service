namespace UKHO.ExchangeSetService.Common.Storage
{
    public interface ISalesCatalogueStorageService
    {
        string GetStorageAccountConnectionString(string storageAccountName = null, string storageAccountKey = null);
        
        string GetStorageAccountConnectionString1(string storageAccountName = null, string storageAccountKey = null);

        string GetStorageAccountConnectionString2(string storageAccountName = null, string storageAccountKey = null);

    }
}
