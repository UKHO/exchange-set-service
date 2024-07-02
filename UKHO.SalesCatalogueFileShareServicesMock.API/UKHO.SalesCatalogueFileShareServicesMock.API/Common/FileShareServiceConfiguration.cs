namespace UKHO.SalesCatalogueFileShareServicesMock.API.Common
{
    public class FileShareServiceConfiguration
    {
        public string FileDirectoryPath { get; set; }
        public string FileDirectoryPathForENC { get; set; }
        public string FileDirectoryPathForReadme { get; set; }
        //To Provide S63 FSS response
        public string S63FssResponseFile { get; set; }
        //To Provide S57 FSS response
        public string S57FssResponseFile { get; set; }
        public string FssInfoResponseFile { get; set; }
        public string FssAdcResponseFile { get; set; }
        public string FolderDirectoryName { get; set; }
        public string DownloadENCFiles307ResponseUri { get; set; }
    }
}
