namespace Jewochron.Services
{
    public class HalachicTimesService
    {
        public (DateTime alotHaShachar, DateTime sunrise, DateTime sunset, DateTime tzait, DateTime chatzot, DateTime minGedolah, DateTime plagHaMincha) CalculateTimes(DateTime date, double latitude, double longitude)
        {
            // Calculate sunrise and sunset based on location
            // This is a more accurate calculation using latitude and day of year
            int dayOfYear = date.DayOfYear;

            // Solar declination
            double declination = 23.45 * Math.Sin(2 * Math.PI * (284 + dayOfYear) / 365);

            // Hour angle at sunrise/sunset
            double latRad = latitude * Math.PI / 180;
            double declRad = declination * Math.PI / 180;
            double hourAngle = Math.Acos(-Math.Tan(latRad) * Math.Tan(declRad));

            // Convert to hours
            double sunriseHour = 12 - (hourAngle * 180 / Math.PI) / 15;
            double sunsetHour = 12 + (hourAngle * 180 / Math.PI) / 15;

            // Adjust for equation of time (simplified)
            double equationOfTime = 9.87 * Math.Sin(2 * 2 * Math.PI * dayOfYear / 365) 
                                  - 7.53 * Math.Cos(2 * Math.PI * dayOfYear / 365) 
                                  - 1.5 * Math.Sin(2 * Math.PI * dayOfYear / 365);

            sunriseHour += equationOfTime / 60;
            sunsetHour += equationOfTime / 60;

            // Adjust for longitude (approximate time zone)
            double timeZoneOffset = Math.Round(longitude / 15);
            sunriseHour -= (longitude / 15 - timeZoneOffset);
            sunsetHour -= (longitude / 15 - timeZoneOffset);

            DateTime sunrise = date.Date.AddHours(sunriseHour);
            DateTime sunset = date.Date.AddHours(sunsetHour);
            DateTime alotHaShachar = sunrise.AddMinutes(-72);
            DateTime tzait = sunset.AddMinutes(42);

            // Calculate Chatzot (midday) - exactly halfway between sunrise and sunset
            TimeSpan dayLength = sunset - sunrise;
            DateTime chatzot = sunrise.Add(dayLength / 2);

            // Mincha Gedolah - 30 minutes after Chatzot
            DateTime minGedolah = chatzot.AddMinutes(30);

            // Plag HaMincha - 1.25 hours before sunset
            DateTime plagHaMincha = sunset.AddHours(-1.25);

            return (alotHaShachar, sunrise, sunset, tzait, chatzot, minGedolah, plagHaMincha);
        }
    }
}
