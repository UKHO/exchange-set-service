﻿namespace UKHO.ExchangeSetService.Common.Models.V2.Request
{
    public class ProductVersionRequest
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public int? UpdateNumber { get; set; }
    }
}
