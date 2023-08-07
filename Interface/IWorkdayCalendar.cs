namespace WorkdayNet.Interface
{
    public interface IWorkdayCalendar
    {
        public void SetHoliday(DateTime date);
        public void SetRecurringHoliday(int month, int day);
        public void SetWorkdayStartAndStop(int startHours, int startMinutes, int stopHours, int stopMinutes);
        public DateTime GetWorkdayIncrement(DateTime startDate, decimal incrementInWorkdays);
    }
}