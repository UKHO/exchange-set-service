using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class SanitizeExtensions
    {
        public static string[] SanitizeProductIdentifiers(this string[] productIdentifiers)
        {
            if (productIdentifiers == null)
            {
                return null;
            }

            if (productIdentifiers.Any(x => x == null))
            {
                return [null];
            }

            var sanitizedIdentifiers = new List<string>();

            if (productIdentifiers.Length > 0)
            {
                foreach (var identifier in productIdentifiers)
                {
                    var sanitizedIdentifier = identifier.Trim();
                    sanitizedIdentifiers.Add(sanitizedIdentifier);
                }
            }

            return [.. sanitizedIdentifiers];
        }

        public static string SanitizeExchangeSetLayout(this string input) => SanitizeEnum(input, ExchangeSetLayout.standard);

        public static string SanitizeExchangeSetStandard(this string input) => SanitizeEnum(input, ExchangeSetStandard.s63);

        private static string SanitizeEnum<T>(string input, T defaultValue) where T : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return defaultValue.ToString();
            }
            else if (Enum.TryParse<T>(input.Trim(), true, out var parsed))
            {
                return parsed.ToString();
            }
            else
            {
                return defaultValue.ToString();
            }
        }

        public static string SanitizeString(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                if (!char.IsControl(ch))
                {
                    sb.Append(ch);
                }
            }

            return sb.ToString();
        }
    }
}
