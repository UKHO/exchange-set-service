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
            return DateTime.TryParseExact(data, "ddMMMyyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
        public static bool IsValidIso8601Format(this string data, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
    }
}
