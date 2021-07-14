using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class FileContentHelper
    {
        public static void CheckSerialEncFileContent(string inputfile)
        {
            string[] lines = File.ReadAllLines(inputfile);

            //Store file content
            string[] fileContent = lines[0].Split(" ");

            string dataServerAndWeek = fileContent[0];
            string dateAndCdType = fileContent[3];
            string formatVersionAndExchangeSetNumber = fileContent[7];

            int weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length-2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}");

            Assert.AreEqual(dateAndCdType,$"{currentDate}UPDATE");

            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"), $"Expected format version {formatVersionAndExchangeSetNumber}");

        }

        public static void CheckProductFileContent(string inputfile, dynamic scsResponse,string ScsJwtToken)
        {
            string[] fileContent = File.ReadAllLines(inputfile);
            int lastIndex = fileContent.Length-2;
            string[] contentFirstLine = fileContent[4].Split(',');
            string[] contentLastLine = fileContent[lastIndex].Split(',');
            int scsResponseLength = scsResponse.Count;
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.True(fileContent[0].Contains(currentDate));
            Assert.True(fileContent[1].Contains("VERSION"));
            Assert.True(fileContent[3].Contains("ENC"));
            //verfying first product details
            Assert.True(contentFirstLine[0].Contains(scsResponse[0].productName.ToString()));
            Assert.True(contentFirstLine[1].Contains(scsResponse[0].baseCellIssueDate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[2].Equals(scsResponse[0].baseCellEditionNumber.ToString()));
            Assert.True(contentFirstLine[3].Contains(scsResponse[0].issueDateLatestUpdate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[4].Equals(scsResponse[0].latestUpdateNumber.ToString()));
            Assert.True(contentFirstLine[5].Equals(scsResponse[0].fileSize.ToString()));
            Assert.True(contentFirstLine[6].Contains(scsResponse[0].cellLimitSouthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[7].Contains(scsResponse[0].cellLimitWesternmostLatitude.ToString()));
            Assert.True(contentFirstLine[8].Contains(scsResponse[0].cellLimitNorthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[9].Contains(scsResponse[0].cellLimitEasternmostLatitude.ToString()));
            //verfying last product details
            Assert.True(contentLastLine[0].Contains(scsResponse[scsResponseLength-1].productName.ToString()));
            Assert.True(contentFirstLine[1].Contains(scsResponse[scsResponseLength - 1].baseCellIssueDate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[2].Equals(scsResponse[scsResponseLength - 1].baseCellEditionNumber.ToString()));
            Assert.True(contentFirstLine[3].Contains(scsResponse[scsResponseLength - 1].issueDateLatestUpdate.ToString("yyyyMMdd")));
            Assert.True(contentFirstLine[4].Equals(scsResponse[scsResponseLength - 1].latestUpdateNumber.ToString()));
            Assert.True(contentFirstLine[5].Equals(scsResponse[scsResponseLength - 1].fileSize.ToString()));
            Assert.True(contentFirstLine[6].Contains(scsResponse[scsResponseLength - 1].cellLimitSouthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[7].Contains(scsResponse[scsResponseLength - 1].cellLimitWesternmostLatitude.ToString()));
            Assert.True(contentFirstLine[8].Contains(scsResponse[scsResponseLength - 1].cellLimitNorthernmostLatitude.ToString()));
            Assert.True(contentFirstLine[9].Contains(scsResponse[scsResponseLength - 1].cellLimitEasternmostLatitude.ToString()));
        }

    }
}
