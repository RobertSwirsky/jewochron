using Microsoft.UI.Xaml.Controls;
using Jewochron.Services;
using Microsoft.UI.Dispatching;

namespace Jewochron.Views
{
    public partial class MainPage : Page
    {
        private readonly HebrewCalendarService hebrewCalendarService;
        private readonly TorahPortionService torahPortionService;
        private readonly DafYomiService dafYomiService;
        private readonly HalachicTimesService halachicTimesService;
        private readonly MoonPhaseService moonPhaseService;
        private readonly LocationService locationService;
        private readonly JewishHolidaysService jewishHolidaysService;
        private readonly MoladService moladService;
        private DispatcherQueueTimer? clockTimer;
        private readonly TimeZoneInfo jerusalemTimeZone;

        public MainPage()
        {
            this.InitializeComponent();

            // Initialize services
            hebrewCalendarService = new HebrewCalendarService();
            torahPortionService = new TorahPortionService(hebrewCalendarService);
            dafYomiService = new DafYomiService(hebrewCalendarService);
            halachicTimesService = new HalachicTimesService();
            moonPhaseService = new MoonPhaseService();
            locationService = new LocationService();
            jewishHolidaysService = new JewishHolidaysService(hebrewCalendarService);
            moladService = new MoladService(hebrewCalendarService);

            // Get Jerusalem time zone
            jerusalemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

            // Start clock timer
            StartClockTimer();

            // Load data
            _ = LoadDataAsync();
        }

        private void StartClockTimer()
        {
            clockTimer = DispatcherQueue.CreateTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) => UpdateClocks();
            clockTimer.Start();

            // Update immediately
            UpdateClocks();
        }

        private void UpdateClocks()
        {
            DateTime now = DateTime.Now;
            DateTime jerusalemTime = TimeZoneInfo.ConvertTime(now, jerusalemTimeZone);

            txtLocalTime.Text = now.ToString("h:mm:ss tt");
            txtJerusalemTime.Text = jerusalemTime.ToString("HH:mm:ss");
        }

        private async Task LoadDataAsync()
        {
            try
            {
                DateTime now = DateTime.Now;

                // Get location
                var (city, state, latitude, longitude) = await locationService.GetLocationAsync();
                txtLocation.Text = $"📍 {city}, {state}";

                // Hebrew date
                var (hebrewYear, hebrewMonth, hebrewDay, isLeapYear) = hebrewCalendarService.GetHebrewDate(now);

                // English date
                txtEnglishDate.Text = now.ToString("dddd, MMMM d, yyyy");

                // Hebrew date in English
                string monthName = hebrewCalendarService.GetHebrewMonthName(hebrewMonth, isLeapYear);
                txtHebrewDate.Text = $"{hebrewDay} {monthName} {hebrewYear}";

                // Hebrew date in Hebrew
                string monthNameHebrew = hebrewCalendarService.GetHebrewMonthNameInHebrew(hebrewMonth, isLeapYear);
                txtHebrewDateInHebrew.Text = $"{hebrewCalendarService.ConvertToHebrewNumber(hebrewDay)} {monthNameHebrew} {hebrewCalendarService.ConvertToHebrewNumber(hebrewYear)}";

                // Next holiday
                var (holidayEnglish, holidayHebrew, holidayDate, daysUntil) = jewishHolidaysService.GetNextHoliday(now);
                txtNextHolidayEnglish.Text = holidayEnglish;
                txtNextHolidayHebrew.Text = holidayHebrew;
                txtDaysUntilHoliday.Text = daysUntil.ToString();
                txtHolidayDate.Text = holidayDate.ToString("MMMM d, yyyy");

                // Torah portion - use async method for accurate results
                var (parshaEnglish, parshaHebrew) = await torahPortionService.GetTorahPortionAsync(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);
                txtParsha.Text = parshaEnglish;
                txtParshaHebrew.Text = parshaHebrew;

                // Daf Yomi
                var (dafYomiEnglish, dafYomiHebrew) = dafYomiService.GetDafYomi(now);
                txtDafYomi.Text = dafYomiEnglish;
                txtDafYomiHebrew.Text = dafYomiHebrew;

                // Halachic times
                var (alotHaShachar, sunrise, sunset, tzait, chatzot, minGedolah, plagHaMincha) = halachicTimesService.CalculateTimes(now, latitude, longitude);
                txtAlotHaShachar.Text = alotHaShachar.ToString("h:mm tt");
                txtSunrise.Text = sunrise.ToString("h:mm tt");
                txtSunset.Text = sunset.ToString("h:mm tt");
                txtTzait.Text = tzait.ToString("h:mm tt");

                // Prayer times
                txtShacharitTime.Text = $"{alotHaShachar.ToString("h:mm tt")} - {chatzot.ToString("h:mm tt")}";
                txtMinchaTime.Text = $"{minGedolah.ToString("h:mm tt")} - {sunset.ToString("h:mm tt")}";
                txtMaarivTime.Text = $"{tzait.ToString("h:mm tt")} onwards";

                // Determine which prayer can be done now (use pointing hand emoji)
                txtShacharitIndicator.Text = "";
                txtMinchaIndicator.Text = "";
                txtMaarivIndicator.Text = "";

                if (now >= alotHaShachar && now < chatzot)
                {
                    txtShacharitIndicator.Text = "👉";
                }
                else if (now >= minGedolah && now < sunset)
                {
                    txtMinchaIndicator.Text = "👉";
                }
                else if (now >= tzait || now < alotHaShachar)
                {
                    txtMaarivIndicator.Text = "👉";
                }

                // Moon phase
                var (moonEmoji, moonPhaseName) = moonPhaseService.GetMoonPhase(now);
                txtMoonPhase.Text = moonEmoji;
                txtMoonPhaseName.Text = moonPhaseName;

                // Molad (New Moon) calculation
                var (moladDateTime, moladDayOfWeek, moladHour, moladChalakim, moladDayName) = moladService.GetNextMolad(now);
                string hebrewDayName = moladService.GetHebrewDayName(moladDayOfWeek);

                // Format molad display
                txtMoladDate.Text = $"{moladDayName} ({hebrewDayName}), {moladDateTime:MMM d}";
                txtMoladTime.Text = $"{moladHour}h {moladChalakim}p (Jerusalem)";
            }
            catch (Exception ex)
            {
                txtLocation.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }
    }
}

