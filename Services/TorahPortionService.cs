namespace Jewochron.Services
{
    public class TorahPortionService
    {
        private readonly HebrewCalendarService hebrewCalendarService;

        public TorahPortionService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (string english, string hebrew) GetTorahPortion(int hebrewYear, int hebrewMonth, int hebrewDay, bool isLeapYear)
        {
            // Find the upcoming or current Saturday
            DateTime today = DateTime.Now;
            DateTime gregorianDate = hebrewCalendarService.ToGregorianDate(hebrewYear, hebrewMonth, hebrewDay);

            // If it's before today, use today instead
            if (gregorianDate < today.Date)
            {
                gregorianDate = today;
            }

            DayOfWeek dayOfWeek = gregorianDate.DayOfWeek;

            // Calculate days until next Saturday (or 0 if today is Saturday and before noon)
            int daysUntilSaturday;
            if (dayOfWeek == DayOfWeek.Saturday && gregorianDate.Date == today.Date && today.TimeOfDay < TimeSpan.FromHours(12))
            {
                daysUntilSaturday = 0; // It's Saturday morning, show this week's parsha
            }
            else if (dayOfWeek == DayOfWeek.Saturday)
            {
                daysUntilSaturday = 7; // Saturday afternoon or past Saturday, get next week
            }
            else
            {
                daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)dayOfWeek + 7) % 7;
            }

            DateTime thisSaturday = gregorianDate.AddDays(daysUntilSaturday);
            var (satYear, saturdayMonth, saturdayDay, satIsLeapYear) = hebrewCalendarService.GetHebrewDate(thisSaturday);

            // Calculate week of year for more accurate parsha determination
            int weekOfYear = GetWeekOfHebrewYear(satYear, saturdayMonth, saturdayDay, satIsLeapYear);

            return GetParshaByWeek(weekOfYear, satIsLeapYear, saturdayMonth);
        }

        private int GetWeekOfHebrewYear(int year, int month, int day, bool isLeapYear)
        {
            // Tishrei 1 is the start of the year
            DateTime roshHashanah = hebrewCalendarService.ToGregorianDate(year, 1, 1);
            DateTime currentDate = hebrewCalendarService.ToGregorianDate(year, month, day);

            // Find the first Saturday of the year
            int daysToFirstSaturday = ((int)DayOfWeek.Saturday - (int)roshHashanah.DayOfWeek + 7) % 7;
            DateTime firstSaturday = roshHashanah.AddDays(daysToFirstSaturday);

            if (currentDate < firstSaturday)
            {
                // Before first Saturday, might be from previous year
                return 54; // Last parsha from previous year
            }

            int daysSinceFirstSaturday = (int)(currentDate - firstSaturday).TotalDays;
            return (daysSinceFirstSaturday / 7) + 1;
        }

        private (string english, string hebrew) GetParshaByWeek(int week, bool isLeapYear, int month)
        {
            // Adjusted parsha cycle based on week of year
            // This accounts for holidays that interrupt the reading cycle

            // During holidays, return special readings
            if (month == 1 && week <= 3)
            {
                return week switch
                {
                    1 => ("Ha'Azinu", "האזינו"),
                    2 => ("Bereishit", "בראשית"),
                    3 => ("Noach", "נח"),
                    _ => ("Bereishit", "בראשית")
                };
            }

            // Regular Torah reading cycle
            return week switch
            {
                1 => ("Ha'Azinu", "האזינו"),
                2 => ("Bereishit", "בראשית"),
                3 => ("Noach", "נח"),
                4 => ("Lech-Lecha", "לך לך"),
                5 => ("Vayera", "וירא"),
                6 => ("Chayei Sarah", "חיי שרה"),
                7 => ("Toldot", "תולדות"),
                8 => ("Vayetzei", "ויצא"),
                9 => ("Vayishlach", "וישלח"),
                10 => ("Vayeshev", "וישב"),
                11 => ("Miketz", "מקץ"),
                12 => ("Vayigash", "ויגש"),
                13 => ("Vayechi", "ויחי"),
                14 => ("Shemot", "שמות"),
                15 => ("Vaera", "וארא"),
                16 => ("Bo", "בא"),
                17 => ("Beshalach", "בשלח"),
                18 => ("Yitro", "יתרו"),
                19 => ("Mishpatim", "משפטים"),
                20 => ("Terumah", "תרומה"),
                21 => ("Tetzaveh", "תצוה"),
                22 => ("Ki Tisa", "כי תשא"),
                23 => ("Vayakhel", "ויקהל"),
                24 => ("Pekudei", "פקודי"),
                25 => ("Vayikra", "ויקרא"),
                26 => ("Tzav", "צו"),
                27 => ("Shemini", "שמיני"),
                28 => ("Tazria", "תזריע"),
                29 => ("Metzora", "מצורע"),
                30 => ("Achrei Mot", "אחרי מות"),
                31 => ("Kedoshim", "קדושים"),
                32 => ("Emor", "אמור"),
                33 => ("Behar", "בהר"),
                34 => ("Bechukotai", "בחוקתי"),
                35 => ("Bamidbar", "במדבר"),
                36 => ("Nasso", "נשא"),
                37 => ("Beha'alotcha", "בהעלתך"),
                38 => ("Sh'lach", "שלח"),
                39 => ("Korach", "קרח"),
                40 => ("Chukat", "חקת"),
                41 => ("Balak", "בלק"),
                42 => ("Pinchas", "פנחס"),
                43 => ("Matot", "מטות"),
                44 => ("Masei", "מסעי"),
                45 => ("Devarim", "דברים"),
                46 => ("Vaetchanan", "ואתחנן"),
                47 => ("Eikev", "עקב"),
                48 => ("Re'eh", "ראה"),
                49 => ("Shoftim", "שופטים"),
                50 => ("Ki Teitzei", "כי תצא"),
                51 => ("Ki Tavo", "כי תבוא"),
                52 => ("Nitzavim", "נצבים"),
                53 => ("Vayeilech", "וילך"),
                _ => ("Bereishit", "בראשית")
            };
        }
    }
}
