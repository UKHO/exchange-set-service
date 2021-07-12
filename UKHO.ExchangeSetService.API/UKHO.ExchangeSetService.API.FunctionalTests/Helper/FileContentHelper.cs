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
            string year = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);
            string currentDate = DateTime.UtcNow.ToString("yyyyMMdd");

            Assert.AreEqual(dataServerAndWeek, $"GBWK{weekNumber}-{year}");

            Assert.AreEqual(dateAndCdType, $"{currentDate}UPDATE");

            Assert.IsTrue(formatVersionAndExchangeSetNumber.StartsWith("02.00U01X01"), $"Expected format version {formatVersionAndExchangeSetNumber}");

        }

        public static void CheckReadMeTxtFileContent(string inputfile)
        {

            string[] lines = File.ReadAllLines(inputfile);
            var fileSecondLineContent = lines[1];

            string[] fileContents = fileSecondLineContent.Split("File date:");

            //Verifying file contents - second line of the readme file
            Assert.True(fileSecondLineContent.Contains(fileContents[0]));

            var utcDateTime = fileContents[1].Remove(fileContents[1].Length - 1);

            Assert.True(DateTime.Parse(utcDateTime) <= new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second), $"Response body returned ExpiryDateTime {utcDateTime} , greater than the expected value.");
        }

    }


}

