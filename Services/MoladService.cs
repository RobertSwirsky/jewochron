namespace Jewochron.Services
{
    public class MoladService
    {
        private readonly HebrewCalendarService hebrewCalendarService;
        
        // Molad Tohu - the theoretical new moon at creation (Monday, 5 hours, 204 chalakim)
        private static readonly DateTime MoladTohu = new DateTime(1, 9, 7, 5, 0, 0); // Approximation
        private const int ChalakimPerHour = 1080;
        private const int ChalakimPerDay = 25920; // 24 * 1080
        
        // Length of average lunar month: 29 days, 12 hours, 793 chalakim
        private const int DaysPerMonth = 29;
        private const int HoursPerMonth = 12;
        private const int ChalakimPerMonth = 793;

        public MoladService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (DateTime dateTime, int dayOfWeek, int hour, int chalakim, string dayName) GetNextMolad(DateTime currentDate)
        {
            var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = hebrewCalendarService.GetHebrewDate(currentDate);
            
            // Get the molad for next month
            int nextMonth = hebrewMonth + 1;
            int yearForMolad = hebrewYear;
            
            // Handle year boundary
            if (!isLeapYear && nextMonth > 12)
            {
                nextMonth = 1;
                yearForMolad++;
            }
            else if (isLeapYear && nextMonth > 13)
            {
                nextMonth = 1;
                yearForMolad++;
            }

            return CalculateMolad(yearForMolad, nextMonth);
        }

        private (DateTime dateTime, int dayOfWeek, int hour, int chalakim, string dayName) CalculateMolad(int hebrewYear, int hebrewMonth)
        {
            // Calculate months since creation (approximately)
            // This is a simplified calculation - for exact results, would need full Jewish calendar algorithm
            int monthsSinceCreation = ((hebrewYear - 1) * 12) + hebrewMonth;
            
            // Account for leap years (7 in every 19 year cycle)
            int cycles = (hebrewYear - 1) / 19;
            int yearInCycle = (hebrewYear - 1) % 19;
            int leapYears = cycles * 7;
            
            // Years 3, 6, 8, 11, 14, 17, 19 in the cycle are leap years
            int[] leapYearPositions = { 2, 5, 7, 10, 13, 16, 18 };
            foreach (int pos in leapYearPositions)
            {
                if (yearInCycle > pos)
                    leapYears++;
            }
            
            monthsSinceCreation += leapYears;

            // Calculate total chalakim since Molad Tohu
            long totalChalakim = (long)monthsSinceCreation * 
                ((DaysPerMonth * ChalakimPerDay) + (HoursPerMonth * ChalakimPerHour) + ChalakimPerMonth);

            // Convert to days, hours, and chalakim
            long days = totalChalakim / ChalakimPerDay;
            long remainder = totalChalakim % ChalakimPerDay;
            int hours = (int)(remainder / ChalakimPerHour);
            int chalakim = (int)(remainder % ChalakimPerHour);

            // Calculate day of week (Monday = 2, Tuesday = 3, etc. in Jewish tradition)
            // Molad Tohu was on Monday (day 2)
            int dayOfWeek = (int)((days + 2) % 7);
            if (dayOfWeek == 0) dayOfWeek = 7; // Saturday

            string dayName = dayOfWeek switch
            {
                1 => "Sunday",
                2 => "Monday", 
                3 => "Tuesday",
                4 => "Wednesday",
                5 => "Thursday",
                6 => "Friday",
                7 => "Saturday",
                _ => "Unknown"
            };

            // Approximate Gregorian date
            DateTime approximateDate = MoladTohu.AddDays(days).AddHours(hours).AddMinutes(chalakim * 3.33 / 60.0);
            
            // Adjust to Jerusalem time (this is approximate)
            try
            {
                var jerusalemTz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                approximateDate = TimeZoneInfo.ConvertTime(approximateDate, jerusalemTz);
            }
            catch
            {
                // If timezone not found, use as is
            }

            return (approximateDate, dayOfWeek, hours, chalakim, dayName);
        }

        public string GetHebrewDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "ראשון",
                2 => "שני",
                3 => "שלישי",
                4 => "רביעי",
                5 => "חמישי",
                6 => "ששי",
                7 => "שבת",
                _ => ""
            };
        }
    }
}
