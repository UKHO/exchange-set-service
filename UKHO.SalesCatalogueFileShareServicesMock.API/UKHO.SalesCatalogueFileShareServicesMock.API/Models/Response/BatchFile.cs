using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class BatchFile
    {
        public string Filename { get; set; }

        public long FileSize { get; set; }

        public string MimeType { get; set; }

        public string Hash { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public Links Links { get; set; }
    }
}