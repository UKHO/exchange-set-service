using System;
using System.Collections.Generic;
using System.Linq;
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

        public static string SanitizeExchangeSetLayout(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ExchangeSetLayout.standard.ToString();
            }

            // Try parse enum (case-insensitive); fallback to "standard"
            if (Enum.TryParse<ExchangeSetLayout>(input.Trim(), ignoreCase: true, out var parsed))
            {
                return parsed.ToString();
            }

            return ExchangeSetLayout.standard.ToString();
        }

        public static string SanitizeExchangeSetStandard(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return ExchangeSetStandard.s63.ToString();
            }

            // Try parse enum (case-insensitive); fallback to "s63"
            if (Enum.TryParse<ExchangeSetStandard>(input.Trim(), ignoreCase: true, out var parsed))
            {
                return parsed.ToString();
            }

            return ExchangeSetStandard.s63.ToString();
        }
    }
}
