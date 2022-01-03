using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class BatchDetail
    {
        public string BatchId { get; set; }

        public string Status { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public string BusinessUnit { get; set; }

        public DateTime? BatchPublishedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public IEnumerable<BatchFile> Files { get; set; }
        
        [JsonIgnore]
        public bool IgnoreCache { get; set; }
    }
}
