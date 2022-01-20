using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Models
{
    public class BatchDetails
    {
        public string Href { get; set; }
    }

    public class BatchStatus
    {
        public string Href { get; set; }
    }

    public class LinksNew
    {
        public BatchDetails BatchDetails { get; set; }
        public BatchStatus BatchStatus { get; set; }
        public GetUrl GetUrl { get; set; }
    }

    public class Attribute
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class GetUrl
    {
        public string Href { get; set; }
    }

    public class CacheFile
    {
        public LinksNew Links { get; set; }
        public string Hash { get; set; }
        public int FileSize { get; set; }
        public string MimeType { get; set; }
        public string Filename { get; set; }
        public List<Attribute> Attributes { get; set; }
    }

    public class EnterpriseEventCacheDataRequest
    {
        public LinksNew Links { get; set; }
        public string BusinessUnit { get; set; }
        public List<Attribute> Attributes { get; set; }
        public List<CacheFile> Files { get; set; }
        public string BatchId { get; set; }
        public DateTime BatchPublishedDate { get; set; }
    }
}
