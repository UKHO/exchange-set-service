// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ErrorDetail
    {
        [JsonPropertyName("source")]
        public string Source { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
