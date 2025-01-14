// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace UKHO.ExchangeSetService.Common.Models.V2
{
    public class ExchangeSetStandardQueueMessage
    {
        public string BatchId { get; set; }
        public long FileSize { get; set; }
        public string ScsResponseUri { get; set; }
        public string CallbackUri { get; set; }
        public string ExchangeSetStandard { get; set; }
        public string CorrelationId { get; set; }
        public string ExchangeSetUrlExpiryDate { get; set; }
        public DateTime ScsRequestDateTime { get; set; }
        public bool IsEmptyExchangeSet { get; set; }
        public int RequestedProductCount { get; set; }
        public int RequestedProductsAlreadyUpToDateCount { get; set; }
        public string ProductIdentifier { get; set; }
        public string Version { get; set; }
    }
}
