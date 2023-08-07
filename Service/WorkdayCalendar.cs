using System.Data;
using System;
using WorkdayNet.Interface;

namespace WorkdayNet.Service
{
    public class WorkdayCalendar : IWorkdayCalendar
    {
        private int workdayStartHours;
        private int workdayStartMinutes;
        private int workdayStopHours;
        private int workdayStopMinutes;

        private List<DateTime> holidays = new List<DateTime>();
        private List<DateTime> recurringHolidays = new List<DateTime>();

        public void SetHoliday(DateTime date)
        {
            this.holidays.Add(date);
        }

        public void SetRecurringHoliday(int month, int day)
        {
            var now = DateTime.Now;
            this.recurringHolidays.Add(new DateTime(now.Year, month, day));
        }

        public void SetWorkdayStartAndStop(int startHours, int startMinutes, int stopHours, int stopMinutes)
        {
            this.workdayStartHours = startHours;
            this.workdayStartMinutes = startMinutes;
            this.workdayStopHours = stopHours;
            this.workdayStopMinutes = stopMinutes;
        }

        private bool IsWeekend(DateTime date)
        {
            DayOfWeek day = date.DayOfWeek;
            return (day == DayOfWeek.Saturday || day == DayOfWeek.Sunday);
        }

        private bool IsHoliday(DateTime date)
        {
            IEnumerable<System.DateTime> holiday = holidays.Where(hd => (date.Year == hd.Year && date.Month == hd.Month && date.Day == hd.Day));

            return holiday.Count() > 0;
        }

        private bool IsRecurringHoliday(DateTime date)
        {
            IEnumerable<System.DateTime> holiday = recurringHolidays.Where(hd => (date.Month == hd.Month && date.Day == hd.Day));

            return holiday.Count() > 0;
        }

        private DateTime GetWorkDayStart(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, this.workdayStartHours, this.workdayStartMinutes, 0);
        }

        private DateTime GetWorkDayStop(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, this.workdayStopHours, this.workdayStopMinutes, 0);
        }

        private DateTime FindNearestWorkday(int direction, DateTime currentDate, DateTime workdayStart, DateTime workdayStop)
        {
            if (this.IsWeekend(currentDate) || this.IsHoliday(currentDate) || this.IsRecurringHoliday(currentDate)) {
                if (direction > 0) {
                    if (currentDate > workdayStop) {
                        currentDate = currentDate.AddDays(direction);
                    }
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStartHours, this.workdayStartMinutes, 0);
                } else {
                    if (currentDate < workdayStart) {
                        currentDate = currentDate.AddDays(direction);
                    }
                    currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStopHours, this.workdayStopMinutes, 0);
                }
            }

            return currentDate;
        }

        private DateTime MoveToWorkday(int direction, DateTime currentDate)
        {
            int currentDay = 0;

            while (currentDay < 1) {
                if (!this.IsWeekend(currentDate) && !this.IsHoliday(currentDate) && !this.IsRecurringHoliday(currentDate)) {
                    currentDay++;
                } else {
                    currentDate = currentDate.AddDays(direction);
                }
            }

            return currentDate;
        }

        private DateTime FindNearestWorkdayHour(int direction, DateTime currentDate, DateTime workdayStart, DateTime workdayStop)
        {
            if (direction > 0 && currentDate >= workdayStop) {
                currentDate = currentDate.AddDays(direction);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStartHours, this.workdayStartMinutes, 0);
            }

            if (direction > 0 && currentDate <= workdayStart) {
                currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStartHours, this.workdayStartMinutes, 0);
            }

            if (direction < 0 && currentDate >= workdayStop) {
                currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStopHours, this.workdayStopMinutes, 0);
            }

            if (direction < 0 && currentDate <= workdayStart) {
                currentDate = currentDate.AddDays(direction);
                currentDate = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, this.workdayStopHours, this.workdayStopMinutes, 0);
            }

            return currentDate;
        }

        private DateTime AddIncrementDays(int direction, int incrementDays, DateTime currentDate)
        {
            int currentDay = 0;

            while (currentDay < incrementDays * direction) {
                currentDate = currentDate.AddDays(direction);
                if (!this.IsWeekend(currentDate) && !this.IsHoliday(currentDate) && !this.IsRecurringHoliday(currentDate)) {
                  currentDay++;
                }
            }

            return currentDate;
        }

        private DateTime AddIncrementFraction(int direction, DateTime currentDate, double workdayFraction)
        {
            currentDate = currentDate.AddHours(workdayFraction);
            
            return currentDate;
        }

        private DateTime SkipNonWorkday(
            int direction, DateTime formattedStartDate, DateTime currentDate, 
            DateTime workdayStart, DateTime workdayStop, DateTime newWorkdayStart, DateTime newWorkdayStop, double freeTimeLength
        )
        {
            if (
                formattedStartDate < workdayStop &&
                formattedStartDate > workdayStart &&
                (currentDate > newWorkdayStop ||
                currentDate < newWorkdayStart)
            ) {
                currentDate.AddMilliseconds(freeTimeLength * direction);
            }
            
            return currentDate;
        }

        public DateTime GetWorkdayIncrement(DateTime startDate, decimal incrementInWorkdays)
        {
            int incrementDays = (int) incrementInWorkdays;
            double incrementFraction = (double)(incrementInWorkdays - incrementDays);
            int direction = incrementInWorkdays > 0 ? 1 : -1;

            DateTime initWorkDayStart = this.GetWorkDayStart(startDate);
            DateTime initWorkDayStop = this.GetWorkDayStop(startDate);

            double workdayLength = (initWorkDayStop - initWorkDayStart).TotalMilliseconds;
            double freeTimeLength = 24 * 60 * 60 * 1000 - workdayLength;
            double workdayFraction = (workdayLength * incrementFraction) / 1000 / 60 / 60;

            DateTime currentDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, startDate.Hour, startDate.Minute, 0);

            currentDate = this.FindNearestWorkday(direction, currentDate, initWorkDayStart, initWorkDayStop);
            currentDate = this.FindNearestWorkdayHour(direction, currentDate, initWorkDayStart, initWorkDayStop);
            currentDate = this.MoveToWorkday(direction, currentDate);
            currentDate = this.AddIncrementDays(direction, incrementDays, currentDate);
            currentDate = this.AddIncrementFraction(direction, currentDate, workdayFraction);

            DateTime newWorkDayStart = this.GetWorkDayStart(currentDate);
            DateTime newWorkDayStop = this.GetWorkDayStop(currentDate);

            currentDate = this.SkipNonWorkday(direction, startDate, currentDate, initWorkDayStart, initWorkDayStop, newWorkDayStart, newWorkDayStop, freeTimeLength);
            currentDate = this.MoveToWorkday(direction, currentDate);

            return currentDate;
        }
    }
}