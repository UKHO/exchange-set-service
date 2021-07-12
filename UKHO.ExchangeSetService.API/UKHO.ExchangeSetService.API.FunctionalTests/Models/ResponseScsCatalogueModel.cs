using System.Collections.Generic;


namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ResponseScsCatalogueModel
    {
        public string ProductName { get; set; }
        public string BaseCellIssueDate { get; set; }
        public int BaseCellEditionNumber { get; set; }
        public string IssueDateLatestUpdate { get; set; }
        public int LatestUpdateNumber { get; set; }
        public int FileSize { get; set; }
        public double CellLimitSouthernmostLatitude { get; set; }
        public double CellLimitWesternmostLatitude { get; set; }
        public double CellLimitNorthernmostLatitude { get; set; }
        public double CellLimitEasternmostLatitude { get; set; }
        public List<DataCoverageCoordinate> DataCoverageCoordinates { get; set; }
        public bool Compression { get; set; }
        public bool Encryption { get; set; }
        public int BaseCellUpdateNumber { get; set; }
        public int LastUpdateNumberPreviousEdition { get; set; }
        public string BaseCellLocation { get; set; }
        public List<string> CancelledCellReplacements { get; set; }
        public string IssueDatePreviousUpdate { get; set; }
    }

    public class DataCoverageCoordinate
    {
        public int Latitude { get; set; }
        public int Longitude { get; set; }
    }
}
