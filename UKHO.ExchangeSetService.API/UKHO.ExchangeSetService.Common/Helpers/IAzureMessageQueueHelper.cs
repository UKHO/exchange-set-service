using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IAzureMessageQueueHelper
    {
        Task AddMessage(IEssFulfilmentStorageConfiguration essFulfilmentStorageConfiguration, string message);
    }
}
