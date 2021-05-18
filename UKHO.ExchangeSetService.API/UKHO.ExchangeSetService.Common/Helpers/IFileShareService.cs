using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareService
    {
        public Task<CreateBatchResponse> CreateBatch();
    }
}
