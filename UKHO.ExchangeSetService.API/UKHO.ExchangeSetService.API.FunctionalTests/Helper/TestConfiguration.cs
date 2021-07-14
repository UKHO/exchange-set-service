using Microsoft.Extensions.Configuration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public class TestConfiguration
    {
        protected IConfigurationRoot ConfigurationRoot;
        public string EssBaseAddress;
        public static string FakeTokenPrivateKey;
        public string ExchangeSetFileName;
        public string ExchangeSetSerialEncFile;
        public string ExchangeReadMeFile;
        public string EssStorageAccountConnectionString;
        public string ExchangeSetProductFile;
        public string ExchangeSetProductFilePath;
        public string ScsBaseAddress;
        public string ExchangeSetProductType;
        public string ExchangeSetCatalogueType;
        public string ExchangeSetEncRootFolder;
        public int FileDownloadWaitTime { get; set; }
        public EssAuthorizationTokenConfiguration EssAuthorizationConfig = new EssAuthorizationTokenConfiguration();
        public FileShareServiceConfiguration FssConfig = new FileShareServiceConfiguration();
        public AzureAdB2CConfiguration AzureAdB2CConfig = new AzureAdB2CConfiguration();
        public SalesCatalogueAuthConfiguration ScsAuthConfig = new SalesCatalogueAuthConfiguration();
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

        public class FileShareServiceConfiguration
        {
            public string FssApiUrl { get; set; }
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string AutoTestClientIdNoAuth { get; set; }
            public string AutoTestClientSecretNoAuth { get; set; }
            public string FssClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
            public int BatchCommitWaitTime { get; set; }
        }

        public class SalesCatalogueAuthConfiguration
        {
            public string MicrosoftOnlineLoginUrl { get; set; }
            public string TenantId { get; set; }
            public string AutoTestClientId { get; set; }
            public string AutoTestClientSecret { get; set; }
            public string AutoTestClientIdNoAuth { get; set; }
            public string AutoTestClientSecretNoAuth { get; set; }
            public string ScsClientId { get; set; }
            public bool IsRunningOnLocalMachine { get; set; }
        }

        public TestConfiguration()
        {
            ConfigurationRoot = new ConfigurationBuilder()
                                .AddJsonFile("appsettings.json", false)
                                .Build();

            EssStorageAccountConnectionString = ConfigurationRoot.GetSection("EssStorageAccountConnectionString").Value;
            EssBaseAddress = ConfigurationRoot.GetSection("EssApiUrl").Value;
            ExchangeSetFileName = ConfigurationRoot.GetSection("ExchangeSetFileName").Value;
            ExchangeSetSerialEncFile = ConfigurationRoot.GetSection("ExchangeSetSerialEncFile").Value;
            ExchangeReadMeFile = ConfigurationRoot.GetSection("ExchangeReadMeFile").Value;
            FakeTokenPrivateKey = ConfigurationRoot.GetSection("FakeTokenPrivateKey").Value;
            ExchangeSetProductFile = ConfigurationRoot.GetSection("ExchangeSetProductFile").Value;
            ExchangeSetProductFilePath = ConfigurationRoot.GetSection("ExchangeSetProductFilePath").Value;
            ExchangeSetProductType = ConfigurationRoot.GetSection("ExchangeSetProductType").Value;
            ExchangeSetCatalogueType = ConfigurationRoot.GetSection("ExchangeSetCatalogueType").Value;
            ScsBaseAddress = ConfigurationRoot.GetSection("ScsApiUrl").Value;
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            ConfigurationRoot.Bind("AzureAdB2CTestConfiguration", AzureAdB2CConfig);          
            ConfigurationRoot.Bind("FileShareServiceConfiguration", FssConfig);
            ExchangeSetEncRootFolder = ConfigurationRoot.GetSection("ExchangeSetEncRootFolder").Value;
            ConfigurationRoot.Bind("SalesCatalogueAuthConfiguration", ScsAuthConfig);
        }
    }
}