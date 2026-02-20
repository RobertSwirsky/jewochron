namespace Jewochron.Services
{
    public class ShabbatTimesService
    {
        private readonly HalachicTimesService halachicTimesService;
        private readonly HebrewCalendarService hebrewCalendarService;

        public ShabbatTimesService(HalachicTimesService halachicTimesService, HebrewCalendarService hebrewCalendarService)
        {
            this.halachicTimesService = halachicTimesService;
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (DateTime candleLighting, DateTime havdalah, DateTime shabbatDate, string parshaName) GetNextShabbatTimes(DateTime now, double latitude, double longitude)
        {
            // Find next Friday
            int daysUntilFriday = ((int)DayOfWeek.Friday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilFriday == 0 && now.DayOfWeek == DayOfWeek.Friday)
            {
                // It's Friday - check if before candle lighting
                var todayTimes = halachicTimesService.CalculateTimes(now, latitude, longitude);
                DateTime todayCandleLighting = todayTimes.sunset.AddMinutes(-18);
                
                if (now.TimeOfDay < todayCandleLighting.TimeOfDay)
                {
                    daysUntilFriday = 0; // Today's Shabbat
                }
                else
                {
                    daysUntilFriday = 7; // Next week's Shabbat
                }
            }
            else if (daysUntilFriday == 0)
            {
                daysUntilFriday = 7; // If somehow 0 but not Friday
            }

            DateTime nextFriday = now.Date.AddDays(daysUntilFriday);
            DateTime nextSaturday = nextFriday.AddDays(1);

            // Calculate times for Friday (candle lighting) and Saturday (Havdalah)
            var fridayTimes = halachicTimesService.CalculateTimes(nextFriday, latitude, longitude);
            var saturdayTimes = halachicTimesService.CalculateTimes(nextSaturday, latitude, longitude);

            // Candle lighting: 18 minutes before sunset on Friday
            DateTime candleLighting = fridayTimes.sunset.AddMinutes(-18);

            // Havdalah: 42 minutes after sunset on Saturday (some communities use 50 or 72)
            DateTime havdalah = saturdayTimes.sunset.AddMinutes(42);

            // Get parsha name (placeholder - would need Torah portion service)
            string parshaName = "Shabbat Shalom";

            return (candleLighting, havdalah, nextSaturday, parshaName);
        }

        public string GetShabbatGreeting()
        {
            return "שבת שלום"; // Shabbat Shalom
        }
    }
}
