using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.CleanUpJob.Services
{
    public interface IExchangeSetCleanUpService
    {
        Task<bool> DeleteHistoricFoldersAndFiles();
    }
}
