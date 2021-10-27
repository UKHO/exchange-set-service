using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{
    public class BatchCommitRequest
    {
        public string FileName { get; set; }

        public string Hash { get; set; }
    }
}
