using System;
using System.Collections.Generic;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public static class ConfigHelper
    {
        public static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int nSize = 30)
        {
            for (int i = 0; i < masterList.Count; i += nSize)
            {
                yield return masterList.GetRange(i, Math.Min(nSize, masterList.Count - i));
            }
        }
    }
}
