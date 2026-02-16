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
        private DispatcherQueueTimer? dataRefreshTimer;
        private readonly TimeZoneInfo jerusalemTimeZone;
        private DateTime lastRefreshDate = DateTime.MinValue;
        private string lastPrayerIndicator = "";

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

            // Start timers
            StartClockTimer();
            StartDataRefreshTimer();

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

        private void StartDataRefreshTimer()
        {
            // Check every minute for data that needs refreshing
            dataRefreshTimer = DispatcherQueue.CreateTimer();
            dataRefreshTimer.Interval = TimeSpan.FromMinutes(1);
            dataRefreshTimer.Tick += async (s, e) => await CheckAndRefreshDataAsync();
            dataRefreshTimer.Start();
        }

        private async Task CheckAndRefreshDataAsync()
        {
            bool needsRefresh = false;
            DateTime now = DateTime.Now;

            // Check if date has changed (past midnight)
            if (lastRefreshDate.Date != now.Date)
            {
                needsRefresh = true;
            }

            // Check if prayer time indicator changed (different prayer period)
            string currentPrayerIndicator = GetCurrentPrayerIndicator(now);
            if (currentPrayerIndicator != lastPrayerIndicator)
            {
                needsRefresh = true;
                lastPrayerIndicator = currentPrayerIndicator;
            }

            // Refresh data if needed
            if (needsRefresh)
            {
                await LoadDataAsync();
            }
        }

        private string GetCurrentPrayerIndicator(DateTime now)
        {
            // Quick check to see which prayer period we're in
            // This is used to detect transitions without recalculating everything
            var hour = now.Hour;

            if (hour >= 5 && hour < 12)
                return "shacharit";
            else if (hour >= 12 && hour < 18)
                return "mincha";
            else
                return "maariv";
        }

        private void UpdateClocks()
        {
            DateTime now = DateTime.Now;
            DateTime jerusalemTime = TimeZoneInfo.ConvertTime(now, jerusalemTimeZone);

            txtLocalTime.Text = now.ToString("h:mm:ss tt");
            txtJerusalemTime.Text = jerusalemTime.ToString("HH:mm:ss");
            
            // Update skyline to reflect Jerusalem time
            UpdateSkyline(jerusalemTime);
        }
        
        private void UpdateSkyline(DateTime jerusalemTime)
        {
            try
            {
                int hour = jerusalemTime.Hour;
                int minute = jerusalemTime.Minute;
                double timeOfDay = hour + (minute / 60.0); // 0-24 as decimal
                
                // Calculate sun/moon positions (moves across sky throughout day)
                // Position range: 100 (left) to 1100 (right)
                double sunPosition = 100 + ((timeOfDay - 6) / 12.0) * 1000; // 6am to 6pm
                double moonPosition = 100 + ((timeOfDay + 12) % 24 / 12.0) * 1000; // Opposite of sun
                
                // Update sun position (clamped to visible range)
                double sunLeft = Math.Clamp(sunPosition, 100, 1100);
                
                // Calculate sun height (arc across sky)
                double sunArc = Math.Sin((timeOfDay - 6) / 12.0 * Math.PI); // 0 at dawn/dusk, 1 at noon
                double sunTop = 80 - (sunArc * 50); // Higher at noon, lower at dawn/dusk
                sunTop = Math.Clamp(sunTop, 20, 80);
                
                // Set sun canvas position
                Canvas.SetLeft(SunCanvas, sunLeft);
                Canvas.SetTop(SunCanvas, sunTop);
                
                // Update moon position
                double moonLeft = Math.Clamp(moonPosition, 100, 1100);
                double moonArc = Math.Sin(((timeOfDay + 12) % 24) / 12.0 * Math.PI);
                double moonTop = 80 - (moonArc * 40);
                moonTop = Math.Clamp(moonTop, 15, 80);
                
                // Set moon canvas position
                Canvas.SetLeft(MoonCanvas, moonLeft);
                Canvas.SetTop(MoonCanvas, moonTop);
                
                // Determine time period and set colors/visibility
                if (timeOfDay >= 5 && timeOfDay < 6) // Dawn (5am-6am)
                {
                    SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.5, 0.4);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Opacity = 1.0 - ((timeOfDay - 5)); // Fade out stars
                }
                else if (timeOfDay >= 6 && timeOfDay < 7) // Sunrise (6am-7am)
                {
                    SetSkyColors("#FF6B6B", "#FFA07A", "#FFD700", 0.6, 0.3);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Collapsed;
                    StarsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 7 && timeOfDay < 10) // Morning (7am-10am)
                {
                    SetSkyColors("#87CEEB", "#B0E0E6", "#E0F6FF", 0.5, 0.3);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Collapsed;
                    StarsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 10 && timeOfDay < 15) // Midday (10am-3pm)
                {
                    SetSkyColors("#4A90E2", "#5DA3E8", "#87CEEB", 0.4, 0.2);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Collapsed;
                    StarsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 15 && timeOfDay < 17) // Afternoon (3pm-5pm)
                {
                    SetSkyColors("#6BA4D8", "#87CEEB", "#B0E0E6", 0.5, 0.3);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Collapsed;
                    StarsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 17 && timeOfDay < 18) // Late afternoon (5pm-6pm)
                {
                    SetSkyColors("#FF8C42", "#FFB366", "#FFD699", 0.6, 0.4);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Collapsed;
                    StarsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 18 && timeOfDay < 19) // Sunset (6pm-7pm)
                {
                    SetSkyColors("#FF6B6B", "#FF8C69", "#FFB347", 0.7, 0.5);
                    SunCanvas.Visibility = Visibility.Visible;
                    MoonCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Opacity = (timeOfDay - 18); // Fade in stars
                }
                else if (timeOfDay >= 19 && timeOfDay < 20) // Dusk (7pm-8pm)
                {
                    SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.6, 0.4);
                    SunCanvas.Visibility = Visibility.Collapsed;
                    MoonCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Opacity = 1.0;
                }
                else // Night (8pm-5am)
                {
                    SetSkyColors("#1A202C", "#2D3748", "#4A5568", 0.7, 0.5);
                    SunCanvas.Visibility = Visibility.Collapsed;
                    MoonCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Visibility = Visibility.Visible;
                    StarsCanvas.Opacity = 1.0;
                }
            }
            catch (Exception)
            {
                // Silently ignore skyline update errors to prevent app crashes
                // The clock will continue to work even if skyline animation fails
            }
        }
        
        private void SetSkyColors(string color1, string color2, string color3, double opacity2, double opacity3)
        {
            try
            {
                SkyLayer1.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color1.Substring(1, 2), 16),
                        Convert.ToByte(color1.Substring(3, 2), 16),
                        Convert.ToByte(color1.Substring(5, 2), 16)));
                        
                SkyLayer2.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color2.Substring(1, 2), 16),
                        Convert.ToByte(color2.Substring(3, 2), 16),
                        Convert.ToByte(color2.Substring(5, 2), 16)));
                SkyLayer2.Opacity = opacity2;
                        
                SkyLayer3.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color3.Substring(1, 2), 16),
                        Convert.ToByte(color3.Substring(3, 2), 16),
                        Convert.ToByte(color3.Substring(5, 2), 16)));
                SkyLayer3.Opacity = opacity3;
            }
            catch (Exception)
            {
                // Silently ignore color update errors
            }
        }
        
        private string ConvertToHalachicTime(DateTime time, DateTime sunrise, DateTime sunset)
        {
            // Halachic day starts at sunrise (hour 1) and has 12 hours until sunset
            // Halachic night starts at sunset (hour 1) and has 12 hours until sunrise
            
            if (time >= sunrise && time < sunset)
            {
                // Daytime halachic hours
                TimeSpan dayLength = sunset - sunrise;
                TimeSpan timeSinceSunrise = time - sunrise;
                double halachicHour = 1 + (timeSinceSunrise.TotalMinutes / dayLength.TotalMinutes * 12);
                
                int hours = (int)halachicHour;
                double minutesFraction = (halachicHour - hours) * 60;
                int minutes = (int)minutesFraction;
                
                return $"Hour {hours}:{minutes:D2} (Day)";
            }
            else
            {
                // Nighttime halachic hours
                DateTime prevSunset = time < sunrise ? sunset.AddDays(-1) : sunset;
                DateTime nextSunrise = time < sunrise ? sunrise : sunrise.AddDays(1);
                
                TimeSpan nightLength = nextSunrise - prevSunset;
                TimeSpan timeSinceSunset = time - prevSunset;
                double halachicHour = 1 + (timeSinceSunset.TotalMinutes / nightLength.TotalMinutes * 12);
                
                int hours = (int)halachicHour;
                double minutesFraction = (halachicHour - hours) * 60;
                int minutes = (int)minutesFraction;
                
                return $"Hour {hours}:{minutes:D2} (Night)";
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                DateTime now = DateTime.Now;
                lastRefreshDate = now.Date;

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
                    lastPrayerIndicator = "shacharit";
                }
                else if (now >= minGedolah && now < sunset)
                {
                    txtMinchaIndicator.Text = "👉";
                    lastPrayerIndicator = "mincha";
                }
                else if (now >= tzait || now < alotHaShachar)
                {
                    txtMaarivIndicator.Text = "👉";
                    lastPrayerIndicator = "maariv";
                }
                else
                {
                    lastPrayerIndicator = "between";
                }

                // Detailed Moon phase with exact illumination
                var (moonEmoji, moonPhaseName, moonIllumination, moonAge) = moonPhaseService.GetDetailedMoonPhase(now);
                txtMoonPhaseIcon.Text = moonEmoji;
                txtMoonPhaseName.Text = moonPhaseName;
                txtMoonIllumination.Text = $"{moonIllumination:F1}% illuminated";
                txtMoonAge.Text = $"Day {Math.Floor(moonAge) + 1} of lunar cycle";

                // Molad (New Moon) calculation
                var (moladDateTime, moladDayOfWeek, moladHour, moladChalakim, moladDayName) = moladService.GetNextMolad(now);
                string hebrewDayName = moladService.GetHebrewDayName(moladDayOfWeek);

                // Format molad display - date with both day names
                txtMoladDate.Text = $"{moladDayName} ({hebrewDayName}), {moladDateTime:MMMM d, yyyy}";
                
                // Convert Molad time to Jerusalem time zone
                TimeZoneInfo jerusalemTz = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
                DateTime moladJerusalemTime = TimeZoneInfo.ConvertTimeFromUtc(moladDateTime, jerusalemTz);
                
                // Display Jerusalem 24-hour time
                txtMoladJerusalemTime.Text = moladJerusalemTime.ToString("HH:mm:ss");
                
                // Calculate Halachic time for the Molad
                // Get sunrise and sunset for that day in Jerusalem
                var (alotMolad, sunriseMolad, sunsetMolad, tzaitMolad, chatzotMolad, minGedolahMolad, plagHaMinchaMolad) = 
                    halachicTimesService.CalculateTimes(moladJerusalemTime.Date, 31.7683, 35.2137); // Jerusalem coordinates
                
                string halachicTime = ConvertToHalachicTime(moladJerusalemTime, sunriseMolad, sunsetMolad);
                txtMoladHalachicTime.Text = halachicTime;
            }
            catch (Exception ex)
            {
                txtLocation.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }
    }
}

