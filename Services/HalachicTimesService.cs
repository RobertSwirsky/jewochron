using Zmanim;
using Zmanim.TimeZone;
using Zmanim.Utilities;

namespace Jewochron.Services
{
    public class HalachicTimesService
    {
        public (DateTime alotHaShachar, DateTime sunrise, DateTime sunset, DateTime tzait, DateTime chatzot, DateTime minGedolah, DateTime plagHaMincha) CalculateTimes(DateTime date, double latitude, double longitude)
        {
            // Create location with WindowsTimeZone
            var location = new GeoLocation("Current Location", latitude, longitude, new WindowsTimeZone(TimeZoneInfo.Local));

            // Create ComplexZmanimCalendar for accurate calculations
            var calendar = new ComplexZmanimCalendar(date, location);

            // Get all the times using the Zmanim library
            DateTime? alotHaShachar = calendar.GetAlos72();
            DateTime? sunrise = calendar.GetSunrise();
            DateTime? sunset = calendar.GetSunset();
            DateTime? tzait = calendar.GetTzais();
            DateTime? chatzot = calendar.GetChatzos();
            DateTime? minGedolah = calendar.GetMinchaGedola();
            DateTime? plagHaMincha = calendar.GetPlagHamincha();

            // Return with fallback values if nulls
            return (
                alotHaShachar ?? date.Date.AddHours(5),
                sunrise ?? date.Date.AddHours(6),
                sunset ?? date.Date.AddHours(18),
                tzait ?? date.Date.AddHours(19),
                chatzot ?? date.Date.AddHours(12),
                minGedolah ?? date.Date.AddHours(12).AddMinutes(30),
                plagHaMincha ?? date.Date.AddHours(17)
            );
        }
    }
}
