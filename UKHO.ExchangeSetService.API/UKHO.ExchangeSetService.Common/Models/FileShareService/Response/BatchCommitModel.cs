using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UKHO.ExchangeSetService.Common.Models.FileShareService.Response
{
    [ExcludeFromCodeCoverage]
    public class BatchCommitModel
    {
        public List<FileDetail> FileDetails { get; set; }
    }
}
