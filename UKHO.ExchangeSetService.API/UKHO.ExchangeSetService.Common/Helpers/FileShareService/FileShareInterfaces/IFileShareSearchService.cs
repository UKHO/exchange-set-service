using System.Collections.Generic;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.Common.Models.FileShareService.Response;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IFileShareSearchService
    {
        Task<string> SearchReadMeFilePath(string batchId, string correlationId);
        Task<IEnumerable<BatchFile>> SearchFolderDetails(string batchId, string correlationId, string uri);
        Task<string> SearchIhoPubFilePath(string batchId, string correlationId);
        Task<string> SearchIhoCrtFilePath(string batchId, string correlationId);
    }
}
