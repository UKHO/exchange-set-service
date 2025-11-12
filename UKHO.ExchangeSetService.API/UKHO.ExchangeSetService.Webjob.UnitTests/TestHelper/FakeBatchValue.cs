using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;
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
        public const string MediaBaseNumber5 = "5";
        /// <summary>
        /// 6
        /// </summary>
        public const string MediaBaseNumber6 = "6";
        /// <summary>
        /// M05X02
        /// </summary>
        public const string LargeExchangeSetFolderName5 = $"M0{MediaBaseNumber5}X02";
        /// <summary>
        /// M05X02.zip
        /// </summary>
        public const string LargeExchangeSetZipFileName5 = $"{LargeExchangeSetFolderName5}.zip";
        /// <summary>
        /// M0{0}X02
        /// </summary>
        public const string LargeExchangeSetFolderNamePattern = "M0{0}X02";
        /// <summary>
        /// M06X02
        /// </summary>
        public const string LargeExchangeSetFolderName6 = $"M0{MediaBaseNumber6}X02";
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
        /// Catalogue
        /// </summary>
        public const string Content = "Catalogue";
        /// <summary>
        /// DVD INFO
        /// </summary>
        public const string ContentInfo = "DVD INFO";
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
        /// ADDS-S57
        /// </summary>
        public const string S57BusinessUnit = "ADDS-S57";
        /// <summary>
        /// ADDS-S63
        /// </summary>
        public const string S63BusinessUnit = "ADDS";
        /// <summary>
        /// 25Sep2025
        /// </summary>
        public const string CurrentUtcDate = "25Sep2025";
        /// <summary>
        /// The DateTime value related to <see cref="CurrentUtcDate"/>
        /// </summary>
        public static DateTime CurrentUtcDateTime { get; }
        /// <summary>
        /// GB800001
        /// </summary>
        public const string AioCell1 = "GB800001";
        /// <summary>
        /// error.txt
        /// </summary>
        public const string ErrorFileName = "error.txt";

        /// <summary>
        /// C:\HOME
        /// </summary>
        public const string BaseDirectoryPath = @"C:\HOME";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272
        /// </summary>
        public const string BatchPath = $@"{BaseDirectoryPath}\{CurrentUtcDate}\{BatchId}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01
        /// </summary>
        public const string ExchangeSetPath = $@"{BatchPath}\{ExchangeSetFileFolder}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT
        /// </summary>
        public const string ExchangeSetEncRootPath = $@"{ExchangeSetPath}\{EncRoot}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\INFO
        /// </summary>
        public const string ExchangeSetInfoPath = $@"{ExchangeSetPath}\{Info}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02
        /// </summary>
        public const string LargeExchangeSetMediaPath5 = $@"{BatchPath}\{LargeExchangeSetFolderName5}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M06X02
        /// </summary>
        public const string LargeExchangeSetMediaPath6 = $@"{BatchPath}\{LargeExchangeSetFolderName6}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\INFO
        /// </summary>
        public const string LargeExchangeSetMediaInfoPath5 = $@"{LargeExchangeSetMediaPath5}\{Info}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M06X02\INFO
        /// </summary>
        public const string LargeExchangeSetMediaInfoPath6 = $@"{LargeExchangeSetMediaPath6}\{Info}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\INFO\ADC
        /// </summary>
        public const string LargeExchangeSetMediaInfoAdcPath5 = $@"{LargeExchangeSetMediaInfoPath5}\{Adc}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M06X02\INFO\ADC
        /// </summary>
        public const string LargeExchangeSetMediaInfoAdcPath6 = $@"{LargeExchangeSetMediaInfoPath6}\{Adc}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\INFO\ENC Update List.csv
        /// </summary>
        public const string UpdateListFilePath5 = $@"{LargeExchangeSetMediaInfoPath5}\{UpdateListFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M05X02\MEDIA.TXT
        /// </summary>
        public const string LargeExchangeSetMediaFilePath5 = $@"{LargeExchangeSetMediaPath5}\{MediaFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\M0{0}X02\{1}\ENC_ROOT
        /// </summary>
        public const string LargeExchangeSetEncRootPattern = $@"{BatchPath}\{LargeExchangeSetFolderNamePattern}\{{1}}\{EncRoot}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\SERIAL.ENC
        /// </summary>
        public const string SerialFilePath = $@"{ExchangeSetPath}\{SerialFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\INFO\PRODUCTS.TXT
        /// </summary>
        public const string ProductFilePath = $@"{ExchangeSetInfoPath}\{ProductFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT\README.TXT
        /// </summary>
        public const string ReadMeFilePath = $@"{ExchangeSetEncRootPath}\{ReadMeFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\ENC_ROOT\CATALOG.031
        /// </summary>
        public const string CatalogFilePath = $@"{ExchangeSetEncRootPath}\{CatalogFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO
        /// </summary>
        public const string AioExchangeSetPath = $@"{BatchPath}\{AioExchangeSetFileFolder}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\ENC_ROOT
        /// </summary>
        public const string AioExchangeSetEncRootPath = $@"{AioExchangeSetPath}\{EncRoot}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\INFO
        /// </summary>
        public const string AioExchangeSetInfoPath = $@"{AioExchangeSetPath}\{Info}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\SERIAL.AIO
        /// </summary>
        public const string SerialAioFilePath = $@"{AioExchangeSetPath}\{SerialAioFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\V01X01\V01X01.zip
        /// </summary>
        public const string ExchangeSetZipFilePath = $@"{BatchPath}\{ExchangeSetZipFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\AIO\AIO.zip
        /// </summary>
        public const string AioExchangeSetZipFilePath = $@"{BatchPath}\{AioExchangeSetZipFileName}";
        /// <summary>
        /// C:\HOME\25Sep2025\7b4cdf10-adfa-4ed6-b2fe-d1543d8b7272\error.txt
        /// </summary>
        public const string ErrorFilePath = $@"{BatchPath}\{ErrorFileName}";

        public static IOptions<FileShareServiceConfiguration> FileShareServiceConfiguration { get; }

        public static IOptions<AioConfiguration> AioConfiguration { get; }

        public static IConfiguration Configuration { get; }

        static FakeBatchValue()
        {
            var currentUtcDateTime = DateTime.ParseExact(CurrentUtcDate, "ddMMMyyyy", CultureInfo.InvariantCulture);
            CurrentUtcDateTime = new DateTime(currentUtcDateTime.Year, currentUtcDateTime.Month, currentUtcDateTime.Day, 13, 14, 15, DateTimeKind.Utc);

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
                Content = Content,
                ContentInfo = ContentInfo,
                EditionNumber = "$batch(EditionNumber) eq '{0}' and ",
                EncRoot = EncRoot,
                ErrorFileName = ErrorFileName,
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
                S57BusinessUnit = S57BusinessUnit,
                S63BusinessUnit = S63BusinessUnit,
                SerialAioFileName = SerialAioFileName,
                SerialFileName = SerialFileName,
                Start = 0,
                UpdateNumber = "$batch(UpdateNumber) eq '{0}' ",
                UpdateNumberLimit = 5
            });

            AioConfiguration = Options.Create(new AioConfiguration
            {
                AioCells = AioCell1
            });

            var inMemSettings = new Dictionary<string, string>
            {
                { "HOME", BaseDirectoryPath }
            };
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemSettings).Build();
        }
    }
}
