using Jewochron.Services;

namespace Jewochron.Models
{
    public class Simcha
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string HebrewDate { get; set; } = "";
        public int HebrewDay { get; set; }
        public int HebrewMonth { get; set; }
        public int HebrewYear { get; set; }
        public DateTime? EnglishDate { get; set; }
        public bool IsRecurring { get; set; } = true;
        public string Notes { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Calculate next occurrence
        public DateTime? GetNextOccurrence(HebrewCalendarService hebrewCalendarService)
        {
            if (IsRecurring)
            {
                // For recurring events, calculate the next Hebrew date anniversary
                return hebrewCalendarService.GetNextHebrewAnniversary(HebrewDay, HebrewMonth, HebrewYear);
            }
            else
            {
                // For non-recurring events, return the original date if it's in the future
                return EnglishDate > DateTime.Now ? EnglishDate : null;
            }
        }
    }

    public enum SimchaType
    {
        HebrewBirthday,
        BarMitzvah,
        BatMitzvah,
        Wedding,
        Engagement,
        BritMilah,
        Pidyon,
        UpSherin,
        Anniversary,
        Other
    }
}