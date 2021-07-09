using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Helper
{
    public static class SerialEncFileHelper
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

    }
}
