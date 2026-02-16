using System.Globalization;

namespace Jewochron.Services
{
    public class HebrewCalendarService
    {
        private readonly HebrewCalendar hebrewCalendar = new();

        public (int year, int month, int day, bool isLeapYear) GetHebrewDate(DateTime date)
        {
            int year = hebrewCalendar.GetYear(date);
            int month = hebrewCalendar.GetMonth(date);
            int day = hebrewCalendar.GetDayOfMonth(date);
            bool isLeapYear = hebrewCalendar.IsLeapYear(year);

            return (year, month, day, isLeapYear);
        }

        public DateTime ToGregorianDate(int hebrewYear, int hebrewMonth, int hebrewDay)
        {
            return hebrewCalendar.ToDateTime(hebrewYear, hebrewMonth, hebrewDay, 0, 0, 0, 0);
        }

        public string GetHebrewMonthName(int month, bool isLeapYear)
        {
            // Use Zmanim month names
            if (isLeapYear)
            {
                return month switch
                {
                    1 => "Tishrei", 2 => "Cheshvan", 3 => "Kislev", 4 => "Tevet",
                    5 => "Shevat", 6 => "Adar I", 7 => "Adar II", 8 => "Nisan",
                    9 => "Iyar", 10 => "Sivan", 11 => "Tammuz", 12 => "Av",
                    13 => "Elul", _ => $"Month {month}"
                };
            }
            else
            {
                return month switch
                {
                    1 => "Tishrei", 2 => "Cheshvan", 3 => "Kislev", 4 => "Tevet",
                    5 => "Shevat", 6 => "Adar", 7 => "Nisan", 8 => "Iyar",
                    9 => "Sivan", 10 => "Tammuz", 11 => "Av", 12 => "Elul",
                    _ => $"Month {month}"
                };
            }
        }

        public string GetHebrewMonthNameInHebrew(int month, bool isLeapYear)
        {
            if (isLeapYear)
            {
                return month switch
                {
                    1 => "תשרי", 2 => "חשוון", 3 => "כסלו", 4 => "טבת",
                    5 => "שבט", 6 => "אדר א׳", 7 => "אדר ב׳", 8 => "ניסן",
                    9 => "אייר", 10 => "סיוון", 11 => "תמוז", 12 => "אב",
                    13 => "אלול", _ => $"חודש {month}"
                };
            }
            else
            {
                return month switch
                {
                    1 => "תשרי", 2 => "חשוון", 3 => "כסלו", 4 => "טבת",
                    5 => "שבט", 6 => "אדר", 7 => "ניסן", 8 => "אייר",
                    9 => "סיוון", 10 => "תמוז", 11 => "אב", 12 => "אלול",
                    _ => $"חודש {month}"
                };
            }
        }

        public string ConvertToHebrewNumber(int number)
        {
            if (number <= 0 || number >= 10000)
                return number.ToString();

            string[] ones = { "", "א", "ב", "ג", "ד", "ה", "ו", "ז", "ח", "ט" };
            string[] tens = { "", "י", "כ", "ל", "מ", "נ", "ס", "ע", "פ", "צ" };
            string[] hundreds = { "", "ק", "ר", "ש", "ת", "תק", "תר", "תש", "תת", "תתק" };
            string[] thousands = { "", "א׳", "ב׳", "ג׳", "ד׳", "ה׳", "ו׳", "ז׳", "ח׳", "ט׳" };

            int thousandsDigit = number / 1000;
            int hundredsDigit = (number % 1000) / 100;
            int tensDigit = (number % 100) / 10;
            int onesDigit = number % 10;

            if (tensDigit == 1 && (onesDigit == 5 || onesDigit == 6))
            {
                string result = thousands[thousandsDigit] + hundreds[hundredsDigit];
                result += (onesDigit == 5) ? "ט״ו" : "ט״ז";
                return result;
            }

            string hebrewNumber = thousands[thousandsDigit] + hundreds[hundredsDigit] + tens[tensDigit] + ones[onesDigit];

            if (hebrewNumber.Length == 1)
                hebrewNumber += "׳";
            else if (hebrewNumber.Length > 1)
                hebrewNumber = hebrewNumber.Insert(hebrewNumber.Length - 1, "״");

            return hebrewNumber;
        }

        public bool IsShabbat(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday;
        }
    }
}
