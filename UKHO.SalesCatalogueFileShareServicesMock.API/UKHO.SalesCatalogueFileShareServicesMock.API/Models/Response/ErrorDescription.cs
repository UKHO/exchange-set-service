// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    [ExcludeFromCodeCoverage]
    public class ErrorDescription
    {
        public string CorrelationId { get; set; }
        public List<Error> Errors { get; set; } = [];
    }

    [ExcludeFromCodeCoverage]
    public class Error
    {
        public string Source { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Source} - {Description}";
        }
    }
}
