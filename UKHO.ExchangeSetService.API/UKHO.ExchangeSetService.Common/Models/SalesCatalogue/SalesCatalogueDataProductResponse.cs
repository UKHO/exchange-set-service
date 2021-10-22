using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Models.SalesCatalogue
{
    public class SalesCatalogueDataProductResponse
    {
        public int? LastUpdateNumberPreviousEdition { get; set; }
        public int? BaseCellUpdateNumber { get; set; }
        public bool Encryption { get; set; }
        public bool Compression { get; set; }
        public string TenDataCoverageCoordinates { get; set; }
        public decimal CellLimitEasternmostLatitude { get; set; }
        public decimal CellLimitNorthernmostLatitude { get; set; }
        public string BaseCellLocation { get; set; }
        public decimal CellLimitWesternmostLatitude { get; set; }
        public int? FileSize { get; set; }
        public int? LatestUpdateNumber { get; set; }
        public DateTime? IssueDateLatestUpdate { get; set; }
        public short BaseCellEditionNumber { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime BaseCellIssueDate { get; set; }
        public string ProductNameLessExtension { get; }
        public string ProductName { get; set; }
        public decimal CellLimitSouthernmostLatitude { get; set; }
        public List<string> CancelledCellReplacements { get; set; }
        public DateTime? IssueDatePreviousUpdate { get; set; }
    }
}
