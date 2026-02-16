using System.Text.Json;

namespace Jewochron.Services
{
    public class TorahPortionService
    {
        private readonly HebrewCalendarService hebrewCalendarService;
        private readonly HttpClient httpClient;

        public TorahPortionService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
            this.httpClient = new HttpClient();
        }

        public async Task<(string english, string hebrew)> GetTorahPortionAsync(int hebrewYear, int hebrewMonth, int hebrewDay, bool isLeapYear)
        {
            try
            {
                // Use HebCal API for accurate parsha
                // Get the upcoming Saturday
                DateTime today = DateTime.Now;
                DateTime gregorianDate = hebrewCalendarService.ToGregorianDate(hebrewYear, hebrewMonth, hebrewDay);

                if (gregorianDate < today.Date)
                {
                    gregorianDate = today.Date;
                }

                DayOfWeek dayOfWeek = gregorianDate.DayOfWeek;

                // Find next Saturday
                int daysUntilSaturday;
                if (dayOfWeek == DayOfWeek.Saturday && gregorianDate.Date == today.Date && today.TimeOfDay < TimeSpan.FromHours(18))
                {
                    daysUntilSaturday = 0; // It's Saturday, show today's parsha
                }
                else if (dayOfWeek == DayOfWeek.Saturday)
                {
                    daysUntilSaturday = 7; // Past Saturday evening, get next week
                }
                else
                {
                    daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)dayOfWeek + 7) % 7;
                }

                DateTime targetSaturday = gregorianDate.AddDays(daysUntilSaturday);

                // Call HebCal API
                string url = $"https://www.hebcal.com/shabbat?cfg=json&gy={targetSaturday.Year}&gm={targetSaturday.Month}&gd={targetSaturday.Day}&M=on&lg=s&m=50";

                var response = await httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);

                // Find the Torah reading
                if (json.RootElement.TryGetProperty("items", out var items))
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        if (item.TryGetProperty("category", out var category) && 
                            category.GetString() == "parashat")
                        {
                            string? title = item.GetProperty("title").GetString();
                            if (!string.IsNullOrEmpty(title))
                            {
                                // Parse "Parashat Terumah" to get "Terumah"
                                string parshaName = title.Replace("Parashat ", "").Trim();
                                string hebrewName = GetHebrewName(parshaName);
                                return (parshaName, hebrewName);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If API fails, fall back to basic calculation
            }

            // Fallback
            return ("Torah Portion", "פרשת השבוע");
        }

        public (string english, string hebrew) GetTorahPortion(int hebrewYear, int hebrewMonth, int hebrewDay, bool isLeapYear)
        {
            // Synchronous wrapper - use async version when possible
            return GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear).GetAwaiter().GetResult();
        }

        private string GetHebrewName(string englishName)
        {
            return englishName switch
            {
                "Bereishit" => "בראשית",
                "Noach" => "נח",
                "Lech-Lecha" => "לך לך",
                "Vayera" => "וירא",
                "Chayei Sara" => "חיי שרה",
                "Toldot" => "תולדות",
                "Vayetzei" => "ויצא",
                "Vayishlach" => "וישלח",
                "Vayeshev" => "וישב",
                "Miketz" => "מקץ",
                "Vayigash" => "ויגש",
                "Vayechi" => "ויחי",
                "Shemot" => "שמות",
                "Vaera" => "וארא",
                "Bo" => "בא",
                "Beshalach" => "בשלח",
                "Yitro" => "יתרו",
                "Mishpatim" => "משפטים",
                "Terumah" => "תרומה",
                "Tetzaveh" => "תצוה",
                "Ki Tisa" => "כי תשא",
                "Vayakhel" => "ויקהל",
                "Pekudei" => "פקודי",
                "Vayakhel-Pekudei" => "ויקהל-פקודי",
                "Vayikra" => "ויקרא",
                "Tzav" => "צו",
                "Shmini" => "שמיני",
                "Tazria" => "תזריע",
                "Metzora" => "מצורע",
                "Tazria-Metzora" => "תזריע-מצורע",
                "Achrei Mot" => "אחרי מות",
                "Kedoshim" => "קדושים",
                "Achrei Mot-Kedoshim" => "אחרי מות-קדושים",
                "Emor" => "אמור",
                "Behar" => "בהר",
                "Bechukotai" => "בחוקתי",
                "Behar-Bechukotai" => "בהר-בחוקתי",
                "Bamidbar" => "במדבר",
                "Nasso" => "נשא",
                "Beha'alotcha" => "בהעלתך",
                "Sh'lach" => "שלח",
                "Korach" => "קרח",
                "Chukat" => "חקת",
                "Balak" => "בלק",
                "Chukat-Balak" => "חקת-בלק",
                "Pinchas" => "פנחס",
                "Matot" => "מטות",
                "Masei" => "מסעי",
                "Matot-Masei" => "מטות-מסעי",
                "Devarim" => "דברים",
                "Vaetchanan" => "ואתחנן",
                "Eikev" => "עקב",
                "Re'eh" => "ראה",
                "Shoftim" => "שופטים",
                "Ki Teitzei" => "כי תצא",
                "Ki Tavo" => "כי תבוא",
                "Nitzavim" => "נצבים",
                "Vayeilech" => "וילך",
                "Nitzavim-Vayeilech" => "נצבים-וילך",
                "Ha'Azinu" => "האזינו",
                "Vezot Haberakhah" => "וזאת הברכה",
                _ => englishName
            };
        }
    }
}
