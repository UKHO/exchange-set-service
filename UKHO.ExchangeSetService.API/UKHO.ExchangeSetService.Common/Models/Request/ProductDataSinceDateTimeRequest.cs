﻿using Swashbuckle.AspNetCore.Annotations;

namespace UKHO.ExchangeSetService.Common.Models.Request
{
    public class ProductDataSinceDateTimeRequest
    {
        [SwaggerSchema(Format = "date-time")]
        public string SinceDateTime { get; set; }

        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
    }
}