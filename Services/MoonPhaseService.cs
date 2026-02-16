namespace Jewochron.Services
{
    public class MoonPhaseService
    {
        public (string emoji, string name) GetMoonPhase(DateTime date)
        {
            DateTime newMoonReference = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
            double synodicMonth = 29.53058867;

            TimeSpan timeSinceReference = date.ToUniversalTime() - newMoonReference;
            double daysSinceReference = timeSinceReference.TotalDays;
            double phase = (daysSinceReference % synodicMonth) / synodicMonth;

            if (phase < 0) phase += 1;

            return phase switch
            {
                < 0.0625 => ("🌑", "New Moon"),
                < 0.1875 => ("🌒", "Waxing Crescent"),
                < 0.3125 => ("🌓", "First Quarter"),
                < 0.4375 => ("🌔", "Waxing Gibbous"),
                < 0.5625 => ("🌕", "Full Moon"),
                < 0.6875 => ("🌖", "Waning Gibbous"),
                < 0.8125 => ("🌗", "Last Quarter"),
                < 0.9375 => ("🌘", "Waning Crescent"),
                _ => ("🌑", "New Moon")
            };
        }
    }
}
