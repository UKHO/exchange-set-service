using System;
using System.Globalization;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static bool IsValidRfc1123Format(this string data, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, "R", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }

        public static bool IsValidDate(this string data, out DateTime dateTime)
        {
            return data.IsValidDate("ddMMMyyyy", out dateTime);
        }

        public static bool IsValidDate(this string data, string format, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
    }
}
