using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class FileCreateModel
    {
        public IEnumerable<KeyValuePair<String, String>> Attributes { get; set; }
    }

}
