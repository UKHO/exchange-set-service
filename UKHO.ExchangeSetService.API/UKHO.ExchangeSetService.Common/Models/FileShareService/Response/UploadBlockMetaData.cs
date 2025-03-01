﻿namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    public class UploadBlockMetaData
    {
        public string BatchId { get; set; }
        public string FullFileName { get; set; }
        public string BlockId { get; set; }
        public long Offset { get; set; }
        public int Length { get; set; }
        public string JwtToken { get; set; }
        public string FileName { get; set; }
    }
}
