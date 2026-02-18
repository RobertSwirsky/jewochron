using Microsoft.EntityFrameworkCore;
using Jewochron.Data;
using Jewochron.Models;

namespace Jewochron.Services
{
    /// <summary>
    /// Service for querying yahrzeit data and checking for upcoming anniversaries
    /// </summary>
    public class YahrzeitService
    {
        private readonly string _databasePath;
        private readonly HebrewCalendarService _hebrewCalendarService;

        public YahrzeitService(string databasePath, HebrewCalendarService hebrewCalendarService)
        {
            _databasePath = databasePath;
            _hebrewCalendarService = hebrewCalendarService;
        }

        /// <summary>
        /// Get yahrzeits occurring today or within the next specified days
        /// </summary>
        public async Task<List<UpcomingYahrzeit>> GetUpcomingYahrzeitsAsync(int daysAhead = 7)
        {
            var upcomingYahrzeits = new List<UpcomingYahrzeit>();

            try
            {
                var options = new DbContextOptionsBuilder<YahrzeitDbContext>()
                    .UseSqlite($"Data Source={_databasePath}")
                    .Options;

                using var dbContext = new YahrzeitDbContext(options);
                var allYahrzeits = await dbContext.Yahrzeits.ToListAsync();

                // Get current Hebrew date
                DateTime today = DateTime.Now;
                var (currentYear, currentMonth, currentDay, isLeapYear) = _hebrewCalendarService.GetHebrewDate(today);

                // Check each yahrzeit to see if it's coming up
                foreach (var yahrzeit in allYahrzeits)
                {
                    for (int daysFromNow = 0; daysFromNow <= daysAhead; daysFromNow++)
                    {
                        DateTime checkDate = today.AddDays(daysFromNow);
                        var (checkYear, checkMonth, checkDay, checkLeapYear) = _hebrewCalendarService.GetHebrewDate(checkDate);

                        // Match month and day (ignoring year since it's an anniversary)
                        if (yahrzeit.HebrewMonth == checkMonth && yahrzeit.HebrewDay == checkDay)
                        {
                            upcomingYahrzeits.Add(new UpcomingYahrzeit
                            {
                                Yahrzeit = yahrzeit,
                                Date = checkDate,
                                DaysFromNow = daysFromNow,
                                HebrewYear = checkYear,
                                HebrewMonth = checkMonth,
                                HebrewDay = checkDay
                            });
                            break; // Found match for this yahrzeit, move to next
                        }
                    }
                }

                // Sort by date (soonest first)
                upcomingYahrzeits.Sort((a, b) => a.DaysFromNow.CompareTo(b.DaysFromNow));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting upcoming yahrzeits: {ex.Message}");
            }

            return upcomingYahrzeits;
        }

        /// <summary>
        /// Get the appropriate honorific for the deceased based on gender
        /// </summary>
        public string GetHonorific(string gender)
        {
            return gender?.ToUpper() == "F" 
                ? "ע״ה" // Aleiha HaShalom (peace be upon her)
                : "ז״ל"; // Zichrono Livrakha (may his memory be a blessing)
        }

        /// <summary>
        /// Get the full honorific phrase
        /// </summary>
        public string GetFullHonorific(string gender)
        {
            return gender?.ToUpper() == "F"
                ? "עליה השלום" // Aleiha HaShalom
                : "זכרונו לברכה"; // Zichrono Livrakha
        }
    }

    /// <summary>
    /// Represents a yahrzeit that is coming up soon
    /// </summary>
    public class UpcomingYahrzeit
    {
        public Yahrzeit Yahrzeit { get; set; } = null!;
        public DateTime Date { get; set; }
        public int DaysFromNow { get; set; }
        public int HebrewYear { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewDay { get; set; }
    }
}
