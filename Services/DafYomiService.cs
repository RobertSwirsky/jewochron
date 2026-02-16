namespace Jewochron.Services
{
    public class DafYomiService
    {
        private readonly HebrewCalendarService hebrewCalendarService;

        public DafYomiService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (string english, string hebrew) GetDafYomi(DateTime date)
        {
            DateTime dafYomiStart = new DateTime(1923, 9, 11);
            int totalPages = 2711;
            
            TimeSpan timeSpan = date - dafYomiStart;
            int daysSinceStart = (int)timeSpan.TotalDays;
            int currentPage = (daysSinceStart % totalPages) + 1;
            
            var (tractate, tractateHebrew, pageInTractate) = GetTractateFromPage(currentPage);
            
            return ($"{tractate} {pageInTractate}", $"{tractateHebrew} {hebrewCalendarService.ConvertToHebrewNumber(pageInTractate)}");
        }

        private (string tractate, string tractateHebrew, int page) GetTractateFromPage(int globalPage)
        {
            var tractates = new[]
            {
                ("Berachot", "ברכות", 63), ("Shabbat", "שבת", 156), ("Eruvin", "עירובין", 104),
                ("Pesachim", "פסחים", 120), ("Shekalim", "שקלים", 21), ("Yoma", "יומא", 87),
                ("Sukkah", "סוכה", 55), ("Beitzah", "ביצה", 39), ("Rosh Hashanah", "ראש השנה", 34),
                ("Taanit", "תענית", 30), ("Megillah", "מגילה", 31), ("Moed Katan", "מועד קטן", 28),
                ("Chagigah", "חגיגה", 26), ("Yevamot", "יבמות", 121), ("Ketubot", "כתובות", 111),
                ("Nedarim", "נדרים", 90), ("Nazir", "נזיר", 65), ("Sotah", "סוטה", 48),
                ("Gittin", "גיטין", 89), ("Kiddushin", "קידושין", 81), ("Bava Kamma", "בבא קמא", 118),
                ("Bava Metzia", "בבא מציעא", 118), ("Bava Batra", "בבא בתרא", 175),
                ("Sanhedrin", "סנהדרין", 113), ("Makkot", "מכות", 23), ("Shevuot", "שבועות", 49),
                ("Avodah Zarah", "עבודה זרה", 75), ("Horayot", "הוריות", 13), ("Zevachim", "זבחים", 119),
                ("Menachot", "מנחות", 109), ("Chullin", "חולין", 141), ("Bechorot", "בכורות", 60),
                ("Arachin", "ערכין", 33), ("Temurah", "תמורה", 33), ("Keritot", "כריתות", 27),
                ("Meilah", "מעילה", 21), ("Niddah", "נדה", 72)
            };

            int cumulativePages = 0;
            foreach (var (name, hebrewName, pages) in tractates)
            {
                if (globalPage <= cumulativePages + pages)
                {
                    return (name, hebrewName, globalPage - cumulativePages);
                }
                cumulativePages += pages;
            }

            return ("Berachot", "ברכות", 1);
        }
    }
}
