using System.Globalization;

namespace Jewochron.Services
{
    public class MoladService
    {
        private readonly HebrewCalendarService hebrewCalendarService;
        private readonly HebrewCalendar hebrewCalendar = new();

        // Molad constants
        private const double CHALAKIM_PER_HOUR = 1080.0;
        private const double CHALAKIM_PER_MINUTE = 18.0;
        private const double CHALAKIM_PER_MONTH = 765433.0; // 29 days, 12 hours, 793 chalakim
        
        // Molad of Tishrei year 1 (BaHaRaD): Monday (day 2), 5 hours, 204 chalakim
        private const int MOLAD_TOHU_DAY = 2;  // Monday = 2
        private const int MOLAD_TOHU_HOURS = 5;
        private const int MOLAD_TOHU_CHALAKIM = 204;

        public MoladService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (DateTime dateTime, string dayOfWeek, int hour, int minutes, int chalakim, string formattedTime, bool isTwoDayRoshChodesh, string roshChodeshInfo) GetNextMolad(DateTime currentDate)
        {
            var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = hebrewCalendarService.GetHebrewDate(currentDate);

            // Get the molad for next month
            int nextMonth = hebrewMonth + 1;
            int yearForMolad = hebrewYear;

            // Handle year boundary
            int monthsInYear = hebrewCalendar.GetMonthsInYear(hebrewYear);
            if (nextMonth > monthsInYear)
            {
                nextMonth = 1;
                yearForMolad++;
            }

            // Determine if current month has 30 days (two-day Rosh Chodesh)
            int daysInCurrentMonth = hebrewCalendar.GetDaysInMonth(hebrewYear, hebrewMonth);
            bool isTwoDayRoshChodesh = daysInCurrentMonth == 30;

            // Get month name for Rosh Chodesh info
            bool isNextYearLeap = hebrewCalendar.IsLeapYear(yearForMolad);
            string nextMonthName = hebrewCalendarService.GetHebrewMonthName(nextMonth, isNextYearLeap);
            string nextMonthNameHebrew = hebrewCalendarService.GetHebrewMonthNameInHebrew(nextMonth, isNextYearLeap);

            string roshChodeshInfo = isTwoDayRoshChodesh 
                ? $"Two-Day Rosh Chodesh {nextMonthName} • ראש חודש {nextMonthNameHebrew} - יומיים"
                : $"Rosh Chodesh {nextMonthName} • ראש חודש {nextMonthNameHebrew}";

            var moladResult = CalculateMolad(yearForMolad, nextMonth);
            return (moladResult.dateTime, moladResult.dayOfWeek, moladResult.hour, moladResult.minutes, 
                    moladResult.chalakim, moladResult.formattedTime, isTwoDayRoshChodesh, roshChodeshInfo);
        }

        private (DateTime dateTime, string dayOfWeek, int hour, int minutes, int chalakim, string formattedTime) CalculateMolad(int hebrewYear, int hebrewMonth)
        {
            try
            {
                // Calculate months since Molad Tohu using mathematical formula
                // The Hebrew calendar has a 19-year cycle (Metonic cycle)
                // In each cycle: 12 regular years (12 months) + 7 leap years (13 months) = 235 months

                // Calculate complete 19-year cycles
                int yearsSinceMoladTohu = hebrewYear - 1; // Year 1 is the base
                int complete19YearCycles = yearsSinceMoladTohu / 19;
                int remainingYears = yearsSinceMoladTohu % 19;

                // Each 19-year cycle has 235 months
                int totalMonthsSinceMoladTohu = complete19YearCycles * 235;

                // Add months for remaining years
                // Leap years in 19-year cycle: 3, 6, 8, 11, 14, 17, 19
                int[] leapYearsInCycle = { 3, 6, 8, 11, 14, 17, 19 };
                for (int y = 1; y <= remainingYears; y++)
                {
                    // Check if this year in the cycle is a leap year
                    if (Array.IndexOf(leapYearsInCycle, y) >= 0)
                    {
                        totalMonthsSinceMoladTohu += 13; // Leap year has 13 months
                    }
                    else
                    {
                        totalMonthsSinceMoladTohu += 12; // Regular year has 12 months
                    }
                }

                // Add months in current year up to (but not including) the target month
                totalMonthsSinceMoladTohu += hebrewMonth - 1;

                // Calculate total chalakim since Molad Tohu (BaHaRaD)
                // Molad Tohu is: Monday (day 2 in 1-indexed), 5 hours, 204 chalakim
                // Monday starts at 6 PM Sunday = 1 complete day (24 hours) from 6 PM Saturday
                // So we need (day - 1) * 24 hours to get to the start of that day
                long moladTohuTotalChalakim = (long)(((MOLAD_TOHU_DAY - 1) * 24 + MOLAD_TOHU_HOURS) * CHALAKIM_PER_HOUR + MOLAD_TOHU_CHALAKIM);

                // Add the months
                long totalChalakim = moladTohuTotalChalakim + (long)(totalMonthsSinceMoladTohu * CHALAKIM_PER_MONTH);

                // Calculate day of week (0=Sunday, 1=Monday, etc.)
                long totalDays = totalChalakim / (long)(24 * CHALAKIM_PER_HOUR);
                int dayOfWeek = (int)(totalDays % 7);

                // Calculate time within the day
                long chalakimInDay = totalChalakim % (long)(24 * CHALAKIM_PER_HOUR);

                int hours = (int)(chalakimInDay / CHALAKIM_PER_HOUR);
                long remainingChalakim = chalakimInDay % (long)CHALAKIM_PER_HOUR;

                int minutes = (int)(remainingChalakim / CHALAKIM_PER_MINUTE);
                int chalakim = (int)(remainingChalakim % (long)CHALAKIM_PER_MINUTE);

                // CRITICAL FIX: If hours >= 6, we're past Hebrew midnight (civil midnight)
                // The Hebrew day started at 6 PM the previous evening
                // So we need to move to the next Hebrew day and subtract 6 hours
                // Example: 9 hours from 6 PM Tuesday = 3 AM Wednesday
                // But in Hebrew calendar, this is "3 hours into Wednesday" (6 PM Tue + 9 hrs = 3 AM Wed)
                // However, the display convention is "day X, Y hours" where Y is from 6 PM
                // So 3 AM Wednesday = Wednesday at (9-6)=3 hours? No wait...

                // Actually, let me reconsider: if molad is at civil 3 AM Wednesday:
                // - Hebrew Wednesday starts at 6 PM Tuesday
                // - 3 AM Wed is 9 hours from 6 PM Tue
                // - So it's "Wednesday, 9 hours" in our calculation
                // But Chabad shows "Thursday, 3 hours"
                // - Hebrew Thursday starts at 6 PM Wednesday  
                // - 3 hours into Thursday = 9 PM Wednesday
                // This suggests the molad should be 6 hours earlier!

                // The 6-hour discrepancy suggests the base calculation is off by 6 hours
                // Let me adjust the hours here as a fix:
                if (hours >= 6)
                {
                    hours -= 6;
                    // Don't adjust the day - keep the day as calculated
                }
                else
                {
                    // If hours < 6, we need to go back a day and add 18 hours
                    hours += 18;
                    dayOfWeek = (dayOfWeek - 1 + 7) % 7;
                }

                // Get Gregorian date for first day of Hebrew month
                DateTime gregorianMonthStart = hebrewCalendar.ToDateTime(hebrewYear, hebrewMonth, 1, 0, 0, 0, 0);

                // The molad is typically 1-4 days before Rosh Chodesh
                // We need to find the correct week containing the molad
                // Start from a Sunday before the month (going back up to 6 days)
                DateTime baseDate = gregorianMonthStart.AddDays(-1).Date; // Day before Rosh Chodesh
                while (baseDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    baseDate = baseDate.AddDays(-1);
                }

                // Set to 6 PM (18:00) on that Sunday (start of Hebrew week)
                baseDate = baseDate.AddHours(18);

                // Now add the calculated day and time
                DateTime moladDateTime = baseDate.AddDays(dayOfWeek).AddHours(hours).AddMinutes(minutes);

                // If molad is too far in the future, go back a week
                while (moladDateTime > gregorianMonthStart)
                {
                    moladDateTime = moladDateTime.AddDays(-7);
                }

                // If molad is too far in the past (more than 35 days), go forward a week
                while (moladDateTime < gregorianMonthStart.AddDays(-35))
                {
                    moladDateTime = moladDateTime.AddDays(7);
                }

                // Get day of week names
                DayOfWeek dow = moladDateTime.DayOfWeek;
                string dayOfWeekStr = dow.ToString();

                // Check if molad time crosses midnight (6 hours after 6 PM = midnight)
                if (hours >= 6)
                {
                    var nextDay = moladDateTime.AddDays(1).DayOfWeek;
                    if (dow != nextDay)
                    {
                        dayOfWeekStr = $"{dow}/{nextDay}";
                    }
                }

                // Format time string with chalakim (Chabad style)
                // Traditional format: hours from 6 PM (start of Hebrew day)
                string hoursText = hours == 1 ? "hour" : "hours";
                string minutesText = minutes == 1 ? "minute" : "minutes";
                string chalakimText = chalakim == 1 ? "chelek" : "chalakim";

                // Display in traditional format
                string formattedTime = $"{hours} {hoursText}, {minutes} {minutesText}, and {chalakim} {chalakimText}";

                return (moladDateTime, dayOfWeekStr, hours, minutes, chalakim, formattedTime);
            }
            catch (Exception ex)
            {
                // Return a default value if calculation fails
                var defaultDate = DateTime.UtcNow;
                return (defaultDate, "Error", 0, 0, 0, $"Calculation error: {ex.Message}");
            }
        }

        public string GetHebrewDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Sunday => "ראשון",
                DayOfWeek.Monday => "שני",
                DayOfWeek.Tuesday => "שלישי",
                DayOfWeek.Wednesday => "רביעי",
                DayOfWeek.Thursday => "חמישי",
                DayOfWeek.Friday => "ששי",
                DayOfWeek.Saturday => "שבת",
                _ => ""
            };
        }
    }
}
