﻿using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

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
        public string ExchangeSetProductType;
        public string ExchangeSetCatalogueType;
        public string ExchangeSetEncRootFolder;
        public string ExchangeSetCatalogueFile;
        
        public EssAuthorizationTokenConfiguration EssAuthorizationConfig = new();
        public FileShareService FssConfig = new();
        public AzureAdB2CConfiguration AzureAdB2CConfig = new();
        public SalesCatalogue ScsAuthConfig = new();
        public CacheConfiguration ClearCacheConfig = new();
        public PeriodicOutputServiceConfiguration POSConfig = new();
        public AioConfiguration AIOConfig = new();
        public BessConfiguration BESSConfig = new();

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

        public class CacheConfiguration
        {
            public string CacheStorageConnectionString { get; set; }
            public string FssSearchCacheTableName { get; set; }
            public string Source { get; set; }
            public string Id { get; set; }
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

        public class PeriodicOutputServiceConfiguration
        {
            public string LargeExchangeSetFolderName1 { get; set; }
            public string LargeExchangeSetFolderName2 { get; set; }
            public string LargeExchangeSetMediaFileName { get; set; }
            public string LargeExchangeSetAdcFolderName { get; set; }
            public string LargeExchangeSetInfoFolderName { get; set; }
            public string ErrorFileName { get; set; }
            public string InfoFolderAvcsUserGuide { get; set; }
            public string InfoFolderEnctandPnmstatus { get; set; }
            public string InfoFolderAddsEul { get; set; }
            public string InfoFolderImpInfo { get; set; }
            public string EncUpdateList { get; set; }
        }

        public class AioConfiguration
        {
            public string AioExchangeSetFileName { get; set; }
            public string ExchangeSetSerialAioFile { get; set; }
            public string AioCellName { get; set; }
            public int AioEditionNumber { get; set; }
            public int AioUpdateNumber { get; set; }
            public string EncCellName { get; set; }
            public int EncEditionNumber { get; set; }
            public int EncUpdateNumber { get; set; }
            public string InvalidEncCellName { get; set; }
            public string InvalidAioCellName { get; set; }
            public string NotModifiedCellName { get; set; }
            public int NotModifiedCellEditionNumber { get; set; }
            public int NotModifiedCellUpdateNumber { get; set; }
        }

        public class BessConfiguration
        {
            public List<string> S63ExchangeSetTestData { get; set; }
            public List<string> S57ExchangeSetTestData { get; set; }
            public List<string> InvalidExchangeSetTestData { get; set; }
            public string TempFolderName { get; set; }
            public string S57BusinessUnit { get; set; }
            public string S63BusinessUnit { get; set; }
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
            ExchangeSetEncRootFolder = ConfigurationRoot.GetSection("ExchangeSetEncRootFolder").Value;
            ExchangeSetCatalogueFile = ConfigurationRoot.GetSection("ExchangeSetCatalogueFile").Value;
            ConfigurationRoot.Bind("EssAuthorizationConfiguration", EssAuthorizationConfig);
            ConfigurationRoot.Bind("AzureAdB2CTestConfiguration", AzureAdB2CConfig);
            ConfigurationRoot.Bind("FileShareService", FssConfig);
            ConfigurationRoot.Bind("SalesCatalogue", ScsAuthConfig);
            ConfigurationRoot.Bind("CacheConfiguration", ClearCacheConfig);
            ConfigurationRoot.Bind("PeriodicOutputServiceConfiguration", POSConfig);
            ConfigurationRoot.Bind("AioConfiguration", AIOConfig);
            ConfigurationRoot.Bind("BessConfiguration", BESSConfig);
        }
    }
}