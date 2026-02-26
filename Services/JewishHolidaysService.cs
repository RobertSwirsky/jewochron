namespace Jewochron.Services
{
    public class JewishHolidaysService
    {
        private readonly HebrewCalendarService hebrewCalendarService;

        public JewishHolidaysService(HebrewCalendarService hebrewCalendarService)
        {
            this.hebrewCalendarService = hebrewCalendarService;
        }

        public (string englishName, string hebrewName, DateTime date, int daysUntil, bool isFast, bool is24HourFast) GetNextHoliday(DateTime currentDate)
        {
            var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = hebrewCalendarService.GetHebrewDate(currentDate);

            // Get all holidays for current and next Hebrew year
            var holidays = GetHolidaysForYear(hebrewYear, isLeapYear)
                .Concat(GetHolidaysForYear(hebrewYear + 1, hebrewCalendarService.GetHebrewDate(currentDate.AddYears(1)).isLeapYear))
                .Where(h => h.date > currentDate)
                .OrderBy(h => h.date)
                .ToList();

            if (holidays.Any())
            {
                var nextHoliday = holidays.First();
                int daysUntil = (int)(nextHoliday.date - currentDate).TotalDays;
                return (nextHoliday.englishName, nextHoliday.hebrewName, nextHoliday.date, daysUntil, nextHoliday.isFast, nextHoliday.is24HourFast);
            }

            return ("No upcoming holidays", "אין חגים קרובים", currentDate, 0, false, false);
        }

        private List<(string englishName, string hebrewName, DateTime date, bool isFast, bool is24HourFast)> GetHolidaysForYear(int hebrewYear, bool isLeapYear)
        {
            var holidays = new List<(string, string, DateTime, bool, bool)>();

            try
            {
                // Tishrei (Month 1)
                holidays.Add(("Rosh Hashanah", "ראש השנה", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 1), false, false));
                holidays.Add(("Rosh Hashanah (Day 2)", "ראש השנה יום ב׳", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 2), false, false));
                holidays.Add(("Fast of Gedaliah", "צום גדליה", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 3), true, false)); // Dawn to dusk
                holidays.Add(("Yom Kippur", "יום כיפור", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 10), true, true)); // 24-hour fast
                holidays.Add(("Sukkot", "סוכות", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 15), false, false));
                holidays.Add(("Sukkot (Day 2)", "סוכות יום ב׳", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 16), false, false));
                holidays.Add(("Hoshana Rabbah", "הושענא רבה", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 21), false, false));
                holidays.Add(("Shemini Atzeret", "שמיני עצרת", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 22), false, false));
                holidays.Add(("Simchat Torah", "שמחת תורה", hebrewCalendarService.ToGregorianDate(hebrewYear, 1, 23), false, false));

                // Kislev (Month 3)
                holidays.Add(("Chanukah (1st candle)", "חנוכה", hebrewCalendarService.ToGregorianDate(hebrewYear, 3, 25), false, false));

                // Tevet (Month 4)
                holidays.Add(("Chanukah (8th day)", "חנוכה יום ח׳", hebrewCalendarService.ToGregorianDate(hebrewYear, 4, 2), false, false));
                holidays.Add(("Fast of Tevet (10th)", "צום עשרה בטבת", hebrewCalendarService.ToGregorianDate(hebrewYear, 4, 10), true, false)); // Dawn to dusk

                // Shevat (Month 5)
                holidays.Add(("Tu B'Shevat", "ט״ו בשבט", hebrewCalendarService.ToGregorianDate(hebrewYear, 5, 15), false, false));

                // Adar/Adar II
                if (isLeapYear)
                {
                    holidays.Add(("Purim Katan", "פורים קטן", hebrewCalendarService.ToGregorianDate(hebrewYear, 6, 14), false, false));
                    holidays.Add(("Fast of Esther", "תענית אסתר", hebrewCalendarService.ToGregorianDate(hebrewYear, 7, 13), true, false)); // Dawn to dusk
                    holidays.Add(("Purim", "פורים", hebrewCalendarService.ToGregorianDate(hebrewYear, 7, 14), false, false));
                    holidays.Add(("Shushan Purim", "שושן פורים", hebrewCalendarService.ToGregorianDate(hebrewYear, 7, 15), false, false));
                }
                else
                {
                    holidays.Add(("Fast of Esther", "תענית אסתר", hebrewCalendarService.ToGregorianDate(hebrewYear, 6, 13), true, false)); // Dawn to dusk
                    holidays.Add(("Purim", "פורים", hebrewCalendarService.ToGregorianDate(hebrewYear, 6, 14), false, false));
                    holidays.Add(("Shushan Purim", "שושן פורים", hebrewCalendarService.ToGregorianDate(hebrewYear, 6, 15), false, false));
                }

                // Nisan (Month 7 in non-leap, 8 in leap)
                int nisanMonth = isLeapYear ? 8 : 7;
                holidays.Add(("Passover (1st day)", "פסח", hebrewCalendarService.ToGregorianDate(hebrewYear, nisanMonth, 15), false, false));
                holidays.Add(("Passover (2nd day)", "פסח יום ב׳", hebrewCalendarService.ToGregorianDate(hebrewYear, nisanMonth, 16), false, false));
                holidays.Add(("Passover (7th day)", "פסח יום ז׳", hebrewCalendarService.ToGregorianDate(hebrewYear, nisanMonth, 21), false, false));
                holidays.Add(("Passover (8th day)", "פסח יום ח׳", hebrewCalendarService.ToGregorianDate(hebrewYear, nisanMonth, 22), false, false));
                holidays.Add(("Yom HaShoah", "יום השואה", hebrewCalendarService.ToGregorianDate(hebrewYear, nisanMonth, 27), false, false));

                // Iyar (Month 8 in non-leap, 9 in leap)
                int iyarMonth = isLeapYear ? 9 : 8;
                holidays.Add(("Yom HaZikaron", "יום הזיכרון", hebrewCalendarService.ToGregorianDate(hebrewYear, iyarMonth, 4), false, false));
                holidays.Add(("Yom HaAtzmaut", "יום העצמאות", hebrewCalendarService.ToGregorianDate(hebrewYear, iyarMonth, 5), false, false));
                holidays.Add(("Lag BaOmer", "ל״ג בעומר", hebrewCalendarService.ToGregorianDate(hebrewYear, iyarMonth, 18), false, false));

                // Sivan (Month 9 in non-leap, 10 in leap)
                int sivanMonth = isLeapYear ? 10 : 9;
                holidays.Add(("Yom Yerushalayim", "יום ירושלים", hebrewCalendarService.ToGregorianDate(hebrewYear, sivanMonth, 28), false, false));
                holidays.Add(("Shavuot (1st day)", "שבועות", hebrewCalendarService.ToGregorianDate(hebrewYear, sivanMonth, 6), false, false));
                holidays.Add(("Shavuot (2nd day)", "שבועות יום ב׳", hebrewCalendarService.ToGregorianDate(hebrewYear, sivanMonth, 7), false, false));

                // Tammuz (Month 10 in non-leap, 11 in leap)
                int tammuzMonth = isLeapYear ? 11 : 10;
                holidays.Add(("Fast of Tammuz (17th)", "צום שבעה עשר בתמוז", hebrewCalendarService.ToGregorianDate(hebrewYear, tammuzMonth, 17), true, false)); // Dawn to dusk

                // Av (Month 11 in non-leap, 12 in leap)
                int avMonth = isLeapYear ? 12 : 11;
                holidays.Add(("Tisha B'Av", "תשעה באב", hebrewCalendarService.ToGregorianDate(hebrewYear, avMonth, 9), true, true)); // 24-hour fast
                holidays.Add(("Tu B'Av", "ט״ו באב", hebrewCalendarService.ToGregorianDate(hebrewYear, avMonth, 15), false, false));
            }
            catch
            {
                // Skip invalid dates
            }

            return holidays;
        }
    }
}
