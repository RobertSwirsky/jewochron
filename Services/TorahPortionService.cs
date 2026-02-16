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
            DateTime gregorianDate = hebrewCalendarService.ToGregorianDate(hebrewYear, hebrewMonth, hebrewDay);
            DayOfWeek dayOfWeek = gregorianDate.DayOfWeek;

            int daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)dayOfWeek + 7) % 7;
            DateTime thisSaturday = gregorianDate.AddDays(daysUntilSaturday);
            
            var (_, saturdayMonth, saturdayDay, _) = hebrewCalendarService.GetHebrewDate(thisSaturday);

            return GetParshaByDate(saturdayMonth, saturdayDay, isLeapYear);
        }

        private (string english, string hebrew) GetParshaByDate(int month, int day, bool isLeapYear)
        {
            return month switch
            {
                1 => day switch { <= 2 => ("Ha'Azinu", "האזינו"), <= 23 => ("Bereishit", "בראשית"), _ => ("Noach", "נח") },
                2 => day switch { <= 7 => ("Noach", "נח"), <= 14 => ("Lech-Lecha", "לך לך"), <= 21 => ("Vayera", "וירא"), _ => ("Chayei Sarah", "חיי שרה") },
                3 => day switch { <= 5 => ("Chayei Sarah", "חיי שרה"), <= 12 => ("Toldot", "תולדות"), <= 19 => ("Vayetzei", "ויצא"), _ => ("Vayishlach", "וישלח") },
                4 => day switch { <= 3 => ("Vayishlach", "וישלח"), <= 10 => ("Vayeshev", "וישב"), <= 17 => ("Miketz", "מקץ"), _ => ("Vayigash", "ויגש") },
                5 => day switch { <= 8 => ("Vayigash", "ויגש"), <= 15 => ("Vayechi", "ויחי"), <= 22 => ("Shemot", "שמות"), _ => ("Vaera", "וארא") },
                6 when !isLeapYear => day switch { <= 6 => ("Vaera", "וארא"), <= 13 => ("Bo", "בא"), <= 20 => ("Beshalach", "בשלח"), _ => ("Yitro", "יתרו") },
                6 when isLeapYear => day switch { <= 6 => ("Vaera", "וארא"), <= 13 => ("Bo", "בא"), <= 20 => ("Beshalach", "בשלח"), _ => ("Yitro", "יתרו") },
                7 when isLeapYear => day switch { <= 6 => ("Mishpatim", "משפטים"), <= 13 => ("Terumah", "תרומה"), <= 20 => ("Tetzaveh", "תצוה"), _ => ("Ki Tisa", "כי תשא") },
                7 when !isLeapYear => day switch { <= 6 => ("Mishpatim", "משפטים"), <= 13 => ("Terumah", "תרומה"), _ => ("Tetzaveh", "תצוה") },
                8 when isLeapYear => day switch { <= 6 => ("Vayakhel-Pekudei", "ויקהל-פקודי"), _ => ("Passover", "פסח") },
                8 when !isLeapYear => day switch { <= 6 => ("Passover", "פסח"), <= 13 => ("Shemini", "שמיני"), <= 20 => ("Tazria-Metzora", "תזריע-מצורע"), _ => ("Achrei Mot-Kedoshim", "אחרי מות-קדושים") },
                9 when isLeapYear => day switch { <= 6 => ("Shemini", "שמיני"), <= 13 => ("Tazria-Metzora", "תזריע-מצורע"), <= 20 => ("Achrei Mot-Kedoshim", "אחרי מות-קדושים"), _ => ("Emor", "אמור") },
                9 when !isLeapYear => day switch { <= 6 => ("Emor", "אמור"), _ => ("Behar-Bechukotai", "בהר-בחוקתי") },
                10 when isLeapYear => day switch { <= 6 => ("Behar-Bechukotai", "בהר-בחוקתי"), <= 13 => ("Bamidbar", "במדבר"), _ => ("Nasso", "נשא") },
                10 when !isLeapYear => day switch { <= 6 => ("Nasso", "נשא"), <= 13 => ("Beha'alotcha", "בהעלתך"), <= 20 => ("Sh'lach", "שלח"), _ => ("Korach", "קרח") },
                11 => day switch { <= 6 => ("Korach", "קרח"), <= 13 => ("Chukat-Balak", "חקת-בלק"), <= 20 => ("Pinchas", "פנחס"), _ => ("Matot-Masei", "מטות-מסעי") },
                12 => day switch { <= 6 => ("Matot-Masei", "מטות-מסעי"), <= 13 => ("Devarim", "דברים"), <= 20 => ("Vaetchanan", "ואתחנן"), _ => ("Eikev", "עקב") },
                13 => day switch { <= 6 => ("Eikev", "עקב"), <= 13 => ("Re'eh", "ראה"), <= 20 => ("Shoftim", "שופטים"), _ => ("Ki Teitzei", "כי תצא") },
                _ => ("Nitzavim-Vayeilech", "נצבים-וילך")
            };
        }
    }
}
