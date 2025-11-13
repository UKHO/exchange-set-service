using System;
using NCrontab;

namespace UKHO.ExchangeSetService.FulfilmentService.Services
{
    public interface IMaintenanceBackgroundService
    {
        (bool Error, string Message, CrontabSchedule Schedule) GetSchedule();
        void RunMaintenance(DateTime utcNow, CrontabSchedule schedule);
    }
}