using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class FileCreateModel
    {
        public IEnumerable<KeyValuePair<String, String>> Attributes { get; set; }
    }

}
