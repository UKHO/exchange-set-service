// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Swashbuckle.AspNetCore.Annotations;

namespace UKHO.ExchangeSetService.Common.Models.V2.Request
{
    public class UpdatesSinceRequest
    {
        [SwaggerSchema(Format = "date-time")]
        [JsonPropertyName("sinceDateTime")]
        public string SinceDateTime { get; set; }
        public string CallbackUri { get; set; }
        public string ProductIdentifier { get; set; } = null;
    }
}
