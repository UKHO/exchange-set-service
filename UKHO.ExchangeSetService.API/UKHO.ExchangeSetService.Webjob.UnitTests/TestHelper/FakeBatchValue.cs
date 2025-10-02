using Microsoft.Extensions.Options;
using UKHO.ExchangeSetService.Common.Configuration;

namespace UKHO.ExchangeSetService.Webjob.UnitTests.TestHelper
{
    internal static class FakeBatchValue
    {
        /// <summary>
        /// 7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272
        /// </summary>
        public const string BatchId = "7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272";
        /// <summary>
        /// 48f53a95-0bd2-4c0c-a6ba-afded2bdffac
        /// </summary>
        public const string CorrelationId = "48f53a95-0bd2-4c0c-a6ba-afded2bdffac";
        /// <summary>
        /// V01X01
        /// </summary>
        public const string ExchangeSetFileFolder = "V01X01";
        /// <summary>
        /// V01X01.zip
        /// </summary>
        public const string ExchangeSetZipFileName = $"{ExchangeSetFileFolder}.zip";
        /// <summary>
        /// AIO
        /// </summary>
        public const string AioExchangeSetFileFolder = "AIO";
        /// <summary>
        /// AIO.zip
        /// </summary>
        public const string AioExchangeSetZipFileName = $"{AioExchangeSetFileFolder}.zip";
        /// <summary>
        /// 5
        /// </summary>
        public const string ExchangeSetMediaBaseNumber = "5";
        /// <summary>
        /// ENC_ROOT
        /// </summary>
        public const string EncRoot = "ENC_ROOT";
        /// <summary>
        /// ADC
        /// </summary>
        public const string Adc = "ADC";
        /// <summary>
        /// INFO
        /// </summary>
        public const string Info = "INFO";
        /// <summary>
        /// README.TXT
        /// </summary>
        public const string ReadMeFileName = "README.TXT";
        /// <summary>
        /// IHO.CRT
        /// </summary>
        public const string IhoCrtFileName = "IHO.CRT";
        /// <summary>
        /// IHO.PUB
        /// </summary>
        public const string IhoPubFileName = "IHO.PUB";
        /// <summary>
        /// PRODUCTS.TXT
        /// </summary>
        public const string ProductFileName = "PRODUCTS.TXT";
        /// <summary>
        /// SERIAL.AIO
        /// </summary>
        public const string SerialAioFileName = "SERIAL.AIO";
        /// <summary>
        /// SERIAL.ENC
        /// </summary>
        public const string SerialFileName = "SERIAL.ENC";
        /// <summary>
        /// CATALOG.031
        /// </summary>
        public const string CatalogFileName = "CATALOG.031";
        /// <summary>
        /// MEDIA.TXT
        /// </summary>
        public const string MediaFileName = "MEDIA.TXT";
        /// <summary>
        /// ENC Update List.csv
        /// </summary>
        public const string UpdateListFileName = "ENC Update List.csv";

        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272
        /// </summary>
        public const string BatchPath = $@"C:\HOME\25SEP2025\{BatchId}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01
        /// </summary>
        public const string ExchangeSetPath = $@"{BatchPath}\{ExchangeSetFileFolder}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT
        /// </summary>
        public const string ExchangeSetEncRootPath = $@"{ExchangeSetPath}\{EncRoot}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\INFO
        /// </summary>
        public const string ExchangeSetInfoPath = $@"{ExchangeSetPath}\{Info}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02
        /// </summary>
        public const string ExchangeSetMediaPath = $@"{BatchPath}\M0{ExchangeSetMediaBaseNumber}X02";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\INFO
        /// </summary>
        public const string ExchangeSetMediaInfoPath = $@"{BatchPath}\M0{ExchangeSetMediaBaseNumber}X02\{Info}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\INFO\ENC Update List.csv
        /// </summary>
        public const string UpdateListFilePath = $@"{ExchangeSetMediaInfoPath}\{UpdateListFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\MEDIA.TXT
        /// </summary>
        public const string ExchangeSetMediaFilePath = $@"{ExchangeSetMediaPath}\{MediaFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\SERIAL.ENC
        /// </summary>
        public const string SerialFilePath = $@"{ExchangeSetPath}\{SerialFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\INFO\PRODUCTS.TXT
        /// </summary>
        public const string ProductFilePath = $@"{ExchangeSetInfoPath}\{ProductFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT\README.TXT
        /// </summary>
        public const string ReadMeFilePath = $@"{ExchangeSetEncRootPath}\{ReadMeFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT\CATALOG.031
        /// </summary>
        public const string CatalogFilePath = $@"{ExchangeSetEncRootPath}\{CatalogFileName}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO
        /// </summary>
        public const string AioExchangeSetPath = $@"{BatchPath}\{AioExchangeSetFileFolder}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\ENC_ROOT
        /// </summary>
        public const string AioExchangeSetEncRootPath = $@"{AioExchangeSetPath}\{EncRoot}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\INFO
        /// </summary>
        public const string AioExchangeSetInfoPath = $@"{AioExchangeSetPath}\{Info}";
        /// <summary>
        /// C:\HOME\25SEP2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\SERIAL.AIO
        /// </summary>
        public const string SerialAioFilePath = $@"{AioExchangeSetPath}\{SerialAioFileName}";

        public static IOptions<FileShareServiceConfiguration> FileShareServiceConfiguration { get; }

        static FakeBatchValue()
        {
            FileShareServiceConfiguration = Options.Create(new FileShareServiceConfiguration
            {
                Adc = Adc,
                AioExchangeSetFileFolder = AioExchangeSetFileFolder,
                AioExchangeSetFileName = AioExchangeSetZipFileName,
                BaseCellExtension = ".000",
                BaseUrl = "https://fss-api.azurewebsites.net",
                BatchCommitCutOffTimeInMinutes = 10,
                BatchCommitDelayTimeInMilliseconds = 5000,
                BlockSizeInMultipleOfKBs = 4096,
                CatalogFileName = CatalogFileName,
                CellName = "$batch(CellName) eq '{0}' and ",
                CommentVersion = "VERSION=1.0",
                Content = "Catalogue",
                ContentInfo = "DVD INFO",
                EditionNumber = "$batch(EditionNumber) eq '{0}' and ",
                EncRoot = EncRoot,
                ErrorFileName = "error.txt",
                EssBusinessUnit = "AVCSCustomExchangeSets",
                ExchangeSetFileFolder = ExchangeSetFileFolder,
                ExchangeSetFileName = ExchangeSetZipFileName,
                IhoCrtFileName = IhoCrtFileName,
                IhoPubFileName = IhoPubFileName,
                Info = Info,
                Limit = 100,
                ParallelSearchTaskCount = 5,
                ParallelUploadThreadCount = 3,
                PosBatchCommitCutOffTimeInMinutes = 20,
                PosBatchCommitDelayTimeInMilliseconds = 10000,
                ProductCode = "$batch(ProductCode) eq 'AVCS' and ",
                ProductFileName = ProductFileName,
                ProductLimit = 4,
                ProductType = "$batch(Product Type) eq 'AVCS' and ",
                PublicBaseUrl = "https://fss-api-public.azurewebsites.net",
                ReadMeFileName = ReadMeFileName,
                ResourceId = "9dd69e65-a953-4a94-a624-c26e6fb33379",
                S57BusinessUnit = "ADDS-S57",
                S63BusinessUnit = "ADDS",
                SerialAioFileName = SerialAioFileName,
                SerialFileName = SerialFileName,
                Start = 0,
                UpdateNumber = "$batch(UpdateNumber) eq '{0}' ",
                UpdateNumberLimit = 5
            });
        }
    }
}
