using Swashbuckle.AspNetCore.Annotations;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace UKHO.SalesCatalogueFileShareServicesMock.API.Models.Request
{
    public class BatchRequest
    {
        public string BusinessUnit { get; set; }

        public Acl ACL { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        [SwaggerSchema(Format = "date-time")]
        public string ExpiryDate { get; set; }

        [JsonIgnore]
        public string CreatedBy { get; set; }
        [JsonIgnore]
        public string CreatedByIssuer { get; set; }
    }
    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }
        public IEnumerable<string> ReadGroups { get; set; }
    }

    public class Attribute
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
