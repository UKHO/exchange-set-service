using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models
{
    public class Products
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
    }

    public class Cancellation
    {
        public int? EditionNumber { get; set; }
        public int? UpdateNumber { get; set; }
    }
}
