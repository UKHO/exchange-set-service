using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Request
{
    [ExcludeFromCodeCoverage]
    public class CreateBatchRequest
    {
        public string BusinessUnit { get; set; }

        public Acl Acl { get; set; }

        public IEnumerable<KeyValuePair<String, string>> Attributes { get; set; }

        public string ExpiryDate { get; set; }
    }
    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }
       
    }
}
