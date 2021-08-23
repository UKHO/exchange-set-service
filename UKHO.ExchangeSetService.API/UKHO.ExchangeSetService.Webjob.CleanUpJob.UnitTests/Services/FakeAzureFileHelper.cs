using System.Threading.Tasks;
using UKHO.ExchangeSetService.CleanUpJob.Helpers;

namespace UKHO.ExchangeSetService.Webjob.CleanUpJob.UnitTests.Services
{
    public class FakeAzureFileHelper : IAzureFileSystemHelper
    {
        public bool DeleteDirectoryAsyncIsCalled = false;

        public async Task<bool> DeleteDirectoryAsync(int numberOfDays, string storageAccountConnectionString, string containerName, string filePath)
        {
            await Task.CompletedTask;
            return DeleteDirectoryAsyncIsCalled = true;
        }
    }
}