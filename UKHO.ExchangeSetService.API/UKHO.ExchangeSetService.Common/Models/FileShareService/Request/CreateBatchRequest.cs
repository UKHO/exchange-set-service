using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Request
{
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
