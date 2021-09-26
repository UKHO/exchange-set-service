using System;
using System.Globalization;

namespace UKHO.ExchangeSetService.API.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsValidRfc1123Format(this string data, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
        public static bool IsValidDate(this string data, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, "ddMMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
    }
} 
