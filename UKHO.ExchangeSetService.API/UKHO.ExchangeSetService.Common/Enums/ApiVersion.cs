// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace UKHO.ExchangeSetService.Common.Enums
{
    public enum ApiVersion
    {
        [EnumMember(Value = "v1")]
        V1,
        [EnumMember(Value = "v2")]
        V2
    }
}
