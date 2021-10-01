using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{
    public class ProductVersionRequest
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public int? UpdateNumber { get; set; }
    }
}
