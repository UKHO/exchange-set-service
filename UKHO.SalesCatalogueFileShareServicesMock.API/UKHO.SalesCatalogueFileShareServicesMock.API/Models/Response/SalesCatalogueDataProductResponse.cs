using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Response
{
    public class SalesCatalogueDataProductResponse
    {
        public short BaseCellEditionNumber { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime BaseCellIssueDate { get; set; }
        public string BaseCellLocation { get; set; }
        public int? BaseCellUpdateNumber { get; set; }
        public List<string> CancelledCellReplacements { get; set; }
        public int? cancelledEditionNumber { get; set; }
        public decimal CellLimitEasternmostLatitude { get; set; }
        public decimal CellLimitNorthernmostLatitude { get; set; }
        public decimal CellLimitSouthernmostLatitude { get; set; }
        public decimal CellLimitWesternmostLatitude { get; set; }
        public bool Compression { get; set; }
        public List<string> dataCoverageCoordinates { get; set; }
        public bool Encryption { get; set; }
        public int? FileSize { get; set; }
        public string id { get; set; }
        public DateTime? IssueDateLatestUpdate { get; set; }
        public DateTime? IssueDatePreviousUpdate { get; set; }
        public int? lastUpdateNumberPreviousEdition { get; set; }
        public int? LatestUpdateNumber { get; set; }
        public string ProductName { get; set; }
    }
}
