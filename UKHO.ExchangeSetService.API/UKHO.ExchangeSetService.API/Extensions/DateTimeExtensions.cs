using System;
using System.Globalization;

namespace UKHO.ExchangeSetService.API.Extensions
{
    public static class DateTimeExtensions
    {
        private const string rfc1123Date = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        public static bool IsValidRfc1123Format(this string data, out DateTime dateTime)
        {
            return DateTime.TryParseExact(data, rfc1123Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
        }
    }
}
