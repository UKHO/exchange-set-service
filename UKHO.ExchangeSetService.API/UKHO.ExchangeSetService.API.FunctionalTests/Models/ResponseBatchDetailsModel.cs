using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class ResponseBatchDetailsModel
    {
        public string BatchId { get; set; }
        public string Status { get; set; }
        [JsonProperty("attributes")]
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
        public string BusinessUnit { get; set; }
        public DateTime? BatchPublishedDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public List<Files> Files { get; set; }
    }

    public class Get
    {
        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class GetLinks
    {
        public Get Get { get; set; }
    }
    public class Files
    {
        [JsonProperty("filename")]
        public string Filename { get; set; }
        [JsonProperty("filesize")]
        public long FileSize { get; set; }
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
        [JsonProperty("hash")]
        public string Hash { get; set; }
        [JsonProperty("attributes")]
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
        [JsonProperty("links")]
        public GetLinks Links { get; set; }
    }
}
