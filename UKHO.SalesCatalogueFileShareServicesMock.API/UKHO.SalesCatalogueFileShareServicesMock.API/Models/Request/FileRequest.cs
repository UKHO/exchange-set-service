using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{  
    public class FileRequest
    {
        public IEnumerable<FileAttribute> Attributes { get; set; }
    }
  
    public class FileAttribute
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
