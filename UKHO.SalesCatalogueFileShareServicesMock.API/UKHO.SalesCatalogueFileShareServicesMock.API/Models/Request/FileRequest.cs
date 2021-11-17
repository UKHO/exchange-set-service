using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{
    public class FileRequest
    {
        public IEnumerable<FileAttribute> Attributes { get; set; }
    }
}