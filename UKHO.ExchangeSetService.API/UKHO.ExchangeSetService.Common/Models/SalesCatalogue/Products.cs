using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class Products
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public List<Dates> Dates { get; set; }
        public Cancellation Cancellation { get; set; }
        public int? FileSize { get; set; }
        [JsonIgnore]
        public bool IgnoreCache { get; set; }
        public List<Bundle> Bundle { get; set; }
    }

    public class Bundle
    {
        public string BundleType { get; set; }
        public string Location { get; set; }
    }
}
