// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.Response
{
    [ExcludeFromCodeCoverage]
    public class ErrorResponse
    {
        public string CorrelationId { get; set; }
        public string Detail { get; set; }
    }
}
