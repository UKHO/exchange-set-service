// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class ErrorResponse
    {
        [JsonPropertyName("correlationId")]
        public string CorrelationId { get; set; }
        [JsonPropertyName("errors")]
        public List<ErrorDetail> Errors { get; set; }
    }
}
