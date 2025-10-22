using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.WebJobs;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentDataService
    {
        Task<string> CreateExchangeSet(FulfilmentServiceBatch batch);
        Task<string> CreateLargeExchangeSet(FulfilmentServiceBatch batch, string largeExchangeSetFolderName);
    }
}
