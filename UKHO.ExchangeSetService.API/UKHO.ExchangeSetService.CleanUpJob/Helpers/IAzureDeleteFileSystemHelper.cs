using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.CleanUpJob.Helpers
{
    public interface IAzureDeleteFileSystemHelper
    {
        Task<bool> DeleteDirectoryAsync(int numberOfDays, string storageAccountConnectionString, string containerName, string filePath);
    }
}
