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
    }
}
