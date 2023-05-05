using System.Collections.Generic;
using System.Net.Http;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class ObjectStorage
    {
        protected ExchangeSetApiClient ExchangeSetApiClient { get; set; }
        protected TestConfiguration Config { get; set; }
        protected string EssJwtToken { get; set; }
        protected ProductIdentifierModel ProductIdentifierModel { get; set; }
        protected DataHelper Datahelper { get; set; }

        protected string FssJwtToken { get; set; }
        public DataHelper DataHelper { get; set; }
        protected HttpResponseMessage ApiEssResponse { get; set; }
       
        protected SalesCatalogueApiClient ScsApiClient { get; set; }
        protected string ScsJwtToken { get; set; }
        protected FssApiClient FssApiClient { get; set; }
        protected List<ProductVersionModel> ProductVersionData { get; set; }

        protected readonly List<string> LargeExchangeSetFolderName = new List<string>();
        protected string batchId;
        protected string DownloadedFolderPath;
    }
}
