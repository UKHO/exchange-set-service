using System;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IMonitorHelper
    {
        void MonitorRequest(string message, DateTime startedAt, DateTime completedAt, string correlationId, int? fileShareServiceSearchQueryCount = null, int? downloadedENCFileCount = null, long? fileSizeInBytes = null, string batchId = null);
    }
}
