// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using UKHO.ExchangeSetService.Common.Configuration;
using UKHO.ExchangeSetService.Common.Logging;
using UKHO.ExchangeSetService.Common.Models.Enums;

namespace UKHO.ExchangeSetService.Common.Extensions
{
    public static class Extensions
    {
        public static string GetBusinessUnit(this string standard, FileShareServiceConfiguration fileShareServiceConfig)
        {
            if (Enum.TryParse(standard, out ExchangeSetStandard exchangeSetStandard))
            {
                var businessUnit = exchangeSetStandard switch
                {
                    ExchangeSetStandard.s63 => fileShareServiceConfig.S63BusinessUnit,
                    ExchangeSetStandard.s57 => fileShareServiceConfig.S57BusinessUnit,
                    _ => throw new FulfilmentException(EventIds.InvalidFssBusinessUnit.ToEventId()) //Exception will be log if fss business unit is not configured
                };

                return businessUnit;
            }

            throw new FulfilmentException(EventIds.InvalidFssBusinessUnit.ToEventId()); //Exception will be log if fss business unit is not configured
        }
    }
}
