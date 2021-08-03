using System;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public interface IMonitorHelper
    {
        void MonitorRequest(string message, DateTime startAt, DateTime completedAt, string correlationId, int? totalHitCountsToFileShareServiceForQuery = null, int? fileCount = null, long? fileSize = null, string batchId = null);
    }
}
