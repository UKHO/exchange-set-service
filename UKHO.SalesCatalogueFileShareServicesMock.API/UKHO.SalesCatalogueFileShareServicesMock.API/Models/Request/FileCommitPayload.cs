using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{
    public class FileCommitPayload
    {
        public IEnumerable<string> BlockIds { get; set; }
    }
}
