using System;
using System.Collections.Generic;
using System.Globalization;

namespace UKHO.ExchangeSetService.Common.Helpers
{
    public static class CommonHelper
    {
        public static IEnumerable<List<T>> SplitList<T>(List<T> masterList, int nSize = 30)
        {
            for (int i = 0; i < masterList.Count; i += nSize)
            {
                yield return masterList.GetRange(i, Math.Min(nSize, masterList.Count - i));
            }
        }

        public static int GetCurrentWeekNumber(DateTime date)
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            return cultureInfo.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);            
        }
    }
}
