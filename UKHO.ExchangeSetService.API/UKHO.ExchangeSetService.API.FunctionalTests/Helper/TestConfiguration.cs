using Microsoft.Extensions.Configuration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public string EssBaseAddress;
        public static string FakeTokenPrivateKey;
        public string ExchangeSetFileName;
        public string ExchangeSetEncRootFolder;
        public string ExchangeSetProductType;
        public string ExchangeSetCatalogueFile;
        public string ExchangeSetProductFile;
        public string ExchangeSetProductFilePath;
        public string EssStorageAccountConnectionString;        
        public int FileDownloadWaitTime { get; set; }
        public EssAuthorizationTokenConfiguration EssAuthorizationConfig = new EssAuthorizationTokenConfiguration();
        public AzureAdB2CConfiguration AzureAdB2CConfig = new AzureAdB2CConfiguration();
        public FileShareService FssConfig = new FileShareService();
        public SalesCatalogue ScsAuthConfig = new SalesCatalogue();
        public class EssAuthorizationTokenConfiguration
        {
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string AutoTestClientIdNoAuth { get; set; }
            public string AutoTestClientSecretNoAuth { get; set; }
            public string EssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class SalesCatalogue
        {
            public string BaseUrl { get; set; }            
            public string ResourceId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public class FileShareService
        {
            public string BaseUrl { get; set; }      
            public string ResourceId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
            public int BatchCommitWaitTime { get; set; }
        }
        public class AzureAdB2CConfiguration
        {
           
            public string ClientId { get; set; }           
            public string Scope { get; set; }           
            public string TenantId { get; set; }            
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string ClientSecret { get; set; }
            // Test Client id is used to test unauthorized scenario for FSS API
            public bool IsRunningOnLocalMachine { get; set; }
            public string LocalTestToken { get; set; } 
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", false)
                                .Build();
            
            EssStorageAccountConnectionString = ConfigurationRoot.GetSection("EssStorageAccountConnectionString").Value;     
            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
            ExchangeSetFileName = ConfigurationRoot.GetSection("ExchangeSetFileName").Value;
            ExchangeSetProductType = ConfigurationRoot.GetSection("ExchangeSetProductType").Value;
            FakeTokenPrivateKey = ConfigurationRoot.GetSection("FakeTokenPrivateKey").Value;
            ExchangeSetEncRootFolder = ConfigurationRoot.GetSection("ExchangeSetEncRootFolder").Value;
            ExchangeSetCatalogueFile = ConfigurationRoot.GetSection("ExchangeSetCatalogueFile").Value;
            ExchangeSetProductFile = ConfigurationRoot.GetSection("ExchangeSetProductFile").Value;
            ExchangeSetProductFilePath = ConfigurationRoot.GetSection("ExchangeSetProductFilePath").Value;
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            ConfigurationRoot.Bind("FileShareService", FssConfig);
            ConfigurationRoot.Bind("AzureAdB2CTestConfiguration", AzureAdB2CConfig);
            ConfigurationRoot.Bind("SalesCatalogue", ScsAuthConfig);
        }
    }
}