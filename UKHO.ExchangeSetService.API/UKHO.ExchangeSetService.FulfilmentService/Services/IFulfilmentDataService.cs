using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IFulfilmentDataService
    {
        Task<string> BuildExchangeSet(string batchid);
    }
}
