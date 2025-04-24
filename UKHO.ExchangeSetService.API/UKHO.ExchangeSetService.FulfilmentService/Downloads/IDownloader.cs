using System.Threading.Tasks;

namespace UKHO.ExchangeSetService.FulfilmentService.Downloads
{
    public interface IDownloader
    {
        Task DownloadInfoFolderFiles(BatchInfo batchInfo);
        Task DownloadAdcFolderFiles(BatchInfo batchInfo);
        Task<bool> DownloadReadMeFileAsync(BatchInfo batchInfo);
        Task<bool> DownloadReadMeFileFromFssAsync(BatchInfo batchInfo);
        Task<bool> DownloadIhoCrtFile(BatchInfo batchInfo);
        Task<bool> DownloadIhoPubFile(BatchInfo batchInfo);
        Task DownloadLargeMediaReadMeFile(BatchInfo batchInfo);
    }
}
