using Cronos;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
namespace VoiceFlow.Application.Helpers
{
    public static class ReportScheduleCalculator
    {
        public static DateTimeOffset? CalculateNextRun(
            Schedule schedule,
            DateTimeOffset nowUtc)
        {
            if (!schedule.Enabled)
                return null;

            var tz = TimeZoneInfo.FindSystemTimeZoneById(
                schedule.Timezone);

            return schedule.Frequency switch
            {
                ScheduleFrequency.Hourly =>
                    CalculateHourly(schedule, nowUtc, tz),

                ScheduleFrequency.Daily =>
                    CalculateDaily(schedule, nowUtc, tz),

                ScheduleFrequency.Weekly =>
                    CalculateWeekly(schedule, nowUtc, tz),

                ScheduleFrequency.Monthly =>
                    CalculateMonthly(schedule, nowUtc, tz),

                ScheduleFrequency.Cron =>
                    CalculateCron(schedule, nowUtc, tz),

                _ => null
            };
        }

        private static DateTimeOffset CalculateHourly(
            Schedule schedule,
            DateTimeOffset nowUtc,
            TimeZoneInfo tz)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);

            var nextLocal = new DateTimeOffset(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                localNow.Hour,
                0,
                0,
                localNow.Offset)
                .AddHours(1);

            return TimeZoneInfo.ConvertTime(nextLocal, TimeZoneInfo.Utc);
        }

        private static DateTimeOffset CalculateDaily(
            Schedule schedule,
            DateTimeOffset nowUtc,
            TimeZoneInfo tz)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);

            var time = TimeOnly.Parse(schedule.RunTime);

            var nextLocal = new DateTime(
                localNow.Year,
                localNow.Month,
                localNow.Day,
                time.Hour,
                time.Minute,
                0);

            if (nextLocal <= localNow.DateTime)
                nextLocal = nextLocal.AddDays(1);

            var offset = tz.GetUtcOffset(nextLocal);

            return new DateTimeOffset(nextLocal, offset)
                .ToUniversalTime();
        }

        private static DateTimeOffset CalculateWeekly(
            Schedule schedule,
            DateTimeOffset nowUtc,
            TimeZoneInfo tz)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);

            var time = TimeOnly.Parse(schedule.RunTime);

            var weekDays = schedule.WeekDays
                .Select(x => Enum.Parse<DayOfWeek>(x))
                .ToHashSet();

            for (var i = 0; i < 14; i++)
            {
                var date = localNow.Date.AddDays(i);

                if (!weekDays.Contains(date.DayOfWeek))
                    continue;

                var candidate = new DateTime(
                    date.Year,
                    date.Month,
                    date.Day,
                    time.Hour,
                    time.Minute,
                    0);

                if (candidate <= localNow.DateTime)
                    continue;

                return new DateTimeOffset(
                    candidate,
                    tz.GetUtcOffset(candidate))
                    .ToUniversalTime();
            }

            throw new InvalidOperationException(
                "No weekly schedule found.");
        }

        private static DateTimeOffset CalculateMonthly(
            Schedule schedule,
            DateTimeOffset nowUtc,
            TimeZoneInfo tz)
        {
            var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);

            var time = TimeOnly.Parse(schedule.RunTime);

            for (var monthOffset = 0; monthOffset < 24; monthOffset++)
            {
                var month = localNow.Date.AddMonths(monthOffset);

                foreach (var day in schedule.MonthDays.Order())
                {
                    if (day > DateTime.DaysInMonth(month.Year, month.Month))
                        continue;

                    var candidate = new DateTime(
                        month.Year,
                        month.Month,
                        day,
                        time.Hour,
                        time.Minute,
                        0);

                    if (candidate <= localNow.DateTime)
                        continue;

                    return new DateTimeOffset(
                        candidate,
                        tz.GetUtcOffset(candidate))
                        .ToUniversalTime();
                }
            }

            throw new InvalidOperationException(
                "No monthly schedule found.");
        }

        private static DateTimeOffset? CalculateCron(
            Schedule schedule,
            DateTimeOffset nowUtc,
            TimeZoneInfo tz)    
        {
            if (string.IsNullOrWhiteSpace(schedule.Cron))
                return null;

            var expression = CronExpression.Parse(
                schedule.Cron,
                CronFormat.IncludeSeconds);

            return expression.GetNextOccurrence(
                nowUtc.UtcDateTime,
                tz,
                inclusive: false);
        }
    }
}
