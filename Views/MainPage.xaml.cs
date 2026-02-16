using Microsoft.UI.Xaml.Controls;
using Jewochron.Services;

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

            // Load data
            _ = LoadDataAsync();
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

                // Torah portion
                var (parshaEnglish, parshaHebrew) = torahPortionService.GetTorahPortion(hebrewYear, hebrewMonth, hebrewDay, isLeapYear);
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
                txtCurrentTime.Text = now.ToString("h:mm tt");
                txtShacharitTime.Text = $"{alotHaShachar.ToString("h:mm tt")} - {chatzot.ToString("h:mm tt")}";
                txtMinchaTime.Text = $"{minGedolah.ToString("h:mm tt")} - {sunset.ToString("h:mm tt")}";
                txtMaarivTime.Text = $"{tzait.ToString("h:mm tt")} onwards";

                // Determine which prayer can be done now
                string currentPrayer = "";
                txtShacharitIndicator.Text = "";
                txtMinchaIndicator.Text = "";
                txtMaarivIndicator.Text = "";

                if (now >= alotHaShachar && now < chatzot)
                {
                    currentPrayer = "Time for Shacharit";
                    txtShacharitIndicator.Text = "✓";
                }
                else if (now >= minGedolah && now < sunset)
                {
                    currentPrayer = "Time for Mincha";
                    txtMinchaIndicator.Text = "✓";
                }
                else if (now >= tzait || now < alotHaShachar)
                {
                    currentPrayer = "Time for Maariv";
                    txtMaarivIndicator.Text = "✓";
                }
                else if (now >= chatzot && now < minGedolah)
                {
                    currentPrayer = "Between prayers";
                }
                else if (now >= sunset && now < tzait)
                {
                    currentPrayer = "Twilight - Wait for Maariv";
                }

                txtCurrentPrayer.Text = currentPrayer;

                // Moon phase
                var (moonEmoji, moonPhaseName) = moonPhaseService.GetMoonPhase(now);
                txtMoonPhase.Text = moonEmoji;
                txtMoonPhaseName.Text = moonPhaseName;
            }
            catch (Exception ex)
            {
                txtLocation.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }
    }
}

