using System.Collections.Generic;
using System.Net.Http;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class ObjectStorage
    {
        protected ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        public TestConfiguration Config { get; set; }
        public string EssJwtToken { get; set; }
        protected ProductIdentifierModel ProductIdentifierModel { get; set; }
        protected DataHelper Datahelper { get; set; }
        public string FssJwtToken { get; set; }
        public DataHelper DataHelper = new();
        protected HttpResponseMessage ApiEssResponse { get; set; }
        protected SalesCatalogueApiClient ScsApiClient { get; set; }
        public string ScsJwtToken { get; set; }
        protected FssApiClient FssApiClient { get; set; }
        protected List<ProductVersionModel> ProductVersionData { get; set; }
        protected readonly List<string> LargeExchangeSetFolderName = new List<string>();
        protected string batchId;
        protected string DownloadedFolderPath;
        protected string AioDownloadedFolderPath;
        protected string EncDownloadedFolderPath;

        public ObjectStorage()
        {
            Config = new TestConfiguration();
            ExchangeSetApiClient = new ExchangeSetApiClient(Config.EssBaseAddress);
            AuthTokenProvider authTokenProvider = new();
            EssJwtToken = authTokenProvider.GetEssToken().Result;
            FssJwtToken = authTokenProvider.GetFssToken().Result;
            ScsApiClient = new SalesCatalogueApiClient(Config.ScsAuthConfig.BaseUrl);
            ScsJwtToken = authTokenProvider.GetScsToken().Result;
            FssApiClient = new FssApiClient();
            ProductVersionData = new List<ProductVersionModel>();
        }
    }
}