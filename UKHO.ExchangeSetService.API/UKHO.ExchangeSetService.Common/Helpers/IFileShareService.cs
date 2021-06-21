using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;
using UKHO.ExchangeSetService.Common.Models.SalesCatalogue;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareService
    {
        public Task<CreateBatchResponse> CreateBatch();
        Task<SearchBatchResponse> GetBatchInfoBasedOnProducts(List<Products> products, string correlationId);
        Task<bool> DownloadBatchFiles(IEnumerable<string> uri, string downloadPath);
    }
}
