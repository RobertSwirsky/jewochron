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
        private DispatcherQueueTimer? camelTimer;
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
            StartCamelAnimation();

            // Load data
            _ = LoadDataAsync();
        }

        private void StartClockTimer()
        {
            clockTimer = DispatcherQueue.CreateTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += (s, e) => 
            {
                try
                {
                    UpdateClocks();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Clock update error: {ex.Message}");
                }
            };
            clockTimer.Start();

            // Update immediately
            try
            {
                UpdateClocks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Initial clock update error: {ex.Message}");
            }
        }

        private void StartDataRefreshTimer()
        {
            // Check every minute for data that needs refreshing
            dataRefreshTimer = DispatcherQueue.CreateTimer();
            dataRefreshTimer.Interval = TimeSpan.FromMinutes(1);
            dataRefreshTimer.Tick += async (s, e) => 
            {
                try
                {
                    await CheckAndRefreshDataAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Data refresh error: {ex.Message}");
                }
            };
            dataRefreshTimer.Start();
        }

        private void StartCamelAnimation()
        {
            try
            {
                // Camel walks every 2 minutes
                camelTimer = DispatcherQueue.CreateTimer();
                camelTimer.Interval = TimeSpan.FromMinutes(2);
                camelTimer.Tick += (s, e) => 
                {
                    try
                    {
                        AnimateCamelWalk();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Camel animation error: {ex.Message}");
                    }
                };
                camelTimer.Start();

                // Start first animation after 1 second using a one-shot timer
                var initialTimer = DispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(1);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Starting INITIAL camel animation NOW!");
                        AnimateCamelWalk();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Initial camel animation error: {ex.Message}");
                    }
                };
                initialTimer.Start();
                System.Diagnostics.Debug.WriteLine("Camel animation timer initialized - first animation in 1 second");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start camel animation: {ex.Message}");
            }
        }

        private void AnimateCamelWalk()
        {
            // Get references to the camel elements
            var animatedCamel = this.FindName("AnimatedCamel") as Microsoft.UI.Xaml.Controls.TextBlock;
            var camelTransform = this.FindName("CamelTransform") as Microsoft.UI.Xaml.Media.TranslateTransform;

            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"[CAMEL DEBUG] AnimateCamelWalk called at {DateTime.Now:HH:mm:ss}");

            if (animatedCamel == null || camelTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] ERROR: Elements not found!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Starting SIMPLE direct animation");

            // SUPER SIMPLE: Just make it visible and animate directly
            try
            {
                // Reset to start position
                camelTransform.X = 0;
                animatedCamel.Opacity = 1;  // FULLY VISIBLE

                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Camel is now VISIBLE at opacity 1");
                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Look at the skyline NOW - you should see it!");

                // Create a simple timer to move it - slower, more majestic pace
                var moveTimer = DispatcherQueue.CreateTimer();
                moveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                moveTimer.Tick += (s, e) =>
                {
                    currentX -= 2;  // Move 2 pixels left each tick (slower than before)
                    camelTransform.X = currentX;

                    // After it goes off screen, stop and reset
                    if (currentX < -1300)
                    {
                        moveTimer.Stop();
                        animatedCamel.Opacity = 0;
                        camelTransform.X = 0;
                        System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Animation complete!");
                    }
                };

                moveTimer.Start();
                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Timer animation started!");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CAMEL DEBUG] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
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
                // Check if skyline elements exist
                var sunCanvas = this.FindName("SunCanvas") as Microsoft.UI.Xaml.UIElement;
                var moonCanvas = this.FindName("MoonCanvas") as Microsoft.UI.Xaml.UIElement;
                var starsCanvas = this.FindName("StarsCanvas") as Microsoft.UI.Xaml.UIElement;
                var skyLayer1 = this.FindName("SkyLayer1");
                var skyLayer2 = this.FindName("SkyLayer2");
                var skyLayer3 = this.FindName("SkyLayer3");

                // If any critical element is missing, skip skyline update
                if (sunCanvas == null || moonCanvas == null || skyLayer1 == null)
                {
                    System.Diagnostics.Debug.WriteLine("Skyline elements not found - skipping update");
                    return;
                }

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
                Canvas.SetLeft(sunCanvas, sunLeft);
                Canvas.SetTop(sunCanvas, sunTop);

                // Update moon position
                double moonLeft = Math.Clamp(moonPosition, 100, 1100);
                double moonArc = Math.Sin(((timeOfDay + 12) % 24) / 12.0 * Math.PI);
                double moonTop = 80 - (moonArc * 40);
                moonTop = Math.Clamp(moonTop, 15, 80);

                // Set moon canvas position
                Canvas.SetLeft(moonCanvas, moonLeft);
                Canvas.SetTop(moonCanvas, moonTop);

                // Determine time period and set colors/visibility
                if (timeOfDay >= 5 && timeOfDay < 6) // Dawn (5am-6am)
                {
                    SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.5, 0.4);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Visible;
                    if (starsCanvas != null)
                    {
                        starsCanvas.Visibility = Visibility.Visible;
                        starsCanvas.Opacity = 1.0 - ((timeOfDay - 5)); // Fade out stars
                    }
                }
                else if (timeOfDay >= 6 && timeOfDay < 7) // Sunrise (6am-7am)
                {
                    SetSkyColors("#FF6B6B", "#FFA07A", "#FFD700", 0.6, 0.3);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Collapsed;
                    if (starsCanvas != null)
                        starsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 7 && timeOfDay < 10) // Morning (7am-10am)
                {
                    SetSkyColors("#87CEEB", "#B0E0E6", "#E0F6FF", 0.5, 0.3);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Collapsed;
                    if (starsCanvas != null)
                        starsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 10 && timeOfDay < 15) // Midday (10am-3pm)
                {
                    SetSkyColors("#4A90E2", "#5DA3E8", "#87CEEB", 0.4, 0.2);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Collapsed;
                    if (starsCanvas != null)
                        starsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 15 && timeOfDay < 17) // Afternoon (3pm-5pm)
                {
                    SetSkyColors("#6BA4D8", "#87CEEB", "#B0E0E6", 0.5, 0.3);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Collapsed;
                    if (starsCanvas != null)
                        starsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 17 && timeOfDay < 18) // Late afternoon (5pm-6pm)
                {
                    SetSkyColors("#FF8C42", "#FFB366", "#FFD699", 0.6, 0.4);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Collapsed;
                    if (starsCanvas != null)
                        starsCanvas.Visibility = Visibility.Collapsed;
                }
                else if (timeOfDay >= 18 && timeOfDay < 19) // Sunset (6pm-7pm)
                {
                    SetSkyColors("#FF6B6B", "#FF8C69", "#FFB347", 0.7, 0.5);
                    sunCanvas.Visibility = Visibility.Visible;
                    moonCanvas.Visibility = Visibility.Visible;
                    if (starsCanvas != null)
                    {
                        starsCanvas.Visibility = Visibility.Visible;
                        starsCanvas.Opacity = (timeOfDay - 18); // Fade in stars
                    }
                }
                else if (timeOfDay >= 19 && timeOfDay < 20) // Dusk (7pm-8pm)
                {
                    SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.6, 0.4);
                    sunCanvas.Visibility = Visibility.Collapsed;
                    moonCanvas.Visibility = Visibility.Visible;
                    if (starsCanvas != null)
                    {
                        starsCanvas.Visibility = Visibility.Visible;
                        starsCanvas.Opacity = 1.0;
                    }
                }
                else // Night (8pm-5am)
                {
                    SetSkyColors("#1A202C", "#2D3748", "#4A5568", 0.7, 0.5);
                    sunCanvas.Visibility = Visibility.Collapsed;
                    moonCanvas.Visibility = Visibility.Visible;
                    if (starsCanvas != null)
                    {
                        starsCanvas.Visibility = Visibility.Visible;
                        starsCanvas.Opacity = 1.0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skyline update error: {ex.Message}");
                // Silently ignore skyline update errors to prevent app crashes
                // The clock will continue to work even if skyline animation fails
            }
        }
        
        private void SetSkyColors(string color1, string color2, string color3, double opacity2, double opacity3)
        {
            try
            {
                var skyLayer1 = this.FindName("SkyLayer1") as Microsoft.UI.Xaml.Shapes.Rectangle;
                var skyLayer2 = this.FindName("SkyLayer2") as Microsoft.UI.Xaml.Shapes.Rectangle;
                var skyLayer3 = this.FindName("SkyLayer3") as Microsoft.UI.Xaml.Shapes.Rectangle;

                if (skyLayer1 == null || skyLayer2 == null || skyLayer3 == null)
                {
                    System.Diagnostics.Debug.WriteLine("Sky layer elements not found");
                    return;
                }

                skyLayer1.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color1.Substring(1, 2), 16),
                        Convert.ToByte(color1.Substring(3, 2), 16),
                        Convert.ToByte(color1.Substring(5, 2), 16)));

                skyLayer2.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color2.Substring(1, 2), 16),
                        Convert.ToByte(color2.Substring(3, 2), 16),
                        Convert.ToByte(color2.Substring(5, 2), 16)));
                skyLayer2.Opacity = opacity2;

                skyLayer3.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.ColorHelper.FromArgb(255,
                        Convert.ToByte(color3.Substring(1, 2), 16),
                        Convert.ToByte(color3.Substring(3, 2), 16),
                        Convert.ToByte(color3.Substring(5, 2), 16)));
                skyLayer3.Opacity = opacity3;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sky color update error: {ex.Message}");
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

        private void UpdatePrayerManAttire(DateTime now, DateTime alotHaShachar, DateTime chatzot, 
            DateTime minGedolah, DateTime sunset, DateTime tzait, bool isShabbat)
        {
            try
            {
                var tallitCanvas = this.FindName("TallitCanvas") as Microsoft.UI.Xaml.UIElement;
                var tefillinCanvas = this.FindName("TefillinCanvas") as Microsoft.UI.Xaml.UIElement;

                if (tallitCanvas == null || tefillinCanvas == null)
                {
                    System.Diagnostics.Debug.WriteLine("Prayer man attire elements not found");
                    return;
                }

                // Determine current prayer time
                bool isShacharit = now >= alotHaShachar && now < chatzot;
                bool isMincha = now >= minGedolah && now < sunset;
                bool isMaariv = now >= tzait || now < alotHaShachar;

                // Show tallit only during Shacharit
                tallitCanvas.Visibility = isShacharit ? Visibility.Visible : Visibility.Collapsed;

                // Show tefillin only during Shacharit on weekdays (not Shabbat)
                tefillinCanvas.Visibility = (isShacharit && !isShabbat) ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Prayer man attire update error: {ex.Message}");
                // Silently ignore errors in updating prayer man attire
                // This is a visual element and shouldn't crash the app
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

                // Add day of week
                var holidayDayOfWeek = this.FindName("txtHolidayDayOfWeek") as Microsoft.UI.Xaml.Controls.TextBlock;
                if (holidayDayOfWeek != null)
                {
                    holidayDayOfWeek.Text = holidayDate.ToString("dddd");
                }
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

                // Update prayer man attire based on prayer time and Shabbat
                bool isShabbat = hebrewCalendarService.IsShabbat(now);
                UpdatePrayerManAttire(now, alotHaShachar, chatzot, minGedolah, sunset, tzait, isShabbat);

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
                var (moladDateTime, moladDayOfWeek, moladHour, moladMinutes, moladChalakim, moladFormattedTime, isTwoDayRoshChodesh, roshChodeshInfo) = moladService.GetNextMolad(now);

                // The moladDateTime is already calculated based on the Hebrew calendar
                // We need to ensure it's in Jerusalem time for display
                // Since the Hebrew calendar and molad are Jerusalem-based, treat this as Jerusalem time
                DateTime moladJerusalemTime = DateTime.SpecifyKind(moladDateTime, DateTimeKind.Unspecified);

                // Format molad display with Rosh Chodesh information
                string gregorianMonthName = moladJerusalemTime.ToString("MMMM");
                int gregorianDay = moladJerusalemTime.Day;
                int gregorianYear = moladJerusalemTime.Year;

                // Split English and Hebrew on separate lines, no moon emojis
                // roshChodeshInfo format: "Rosh Chodesh Month • ראש חודש חודש"
                string[] parts = roshChodeshInfo.Split('•');
                string englishPart = parts[0].Trim();
                string hebrewPart = parts.Length > 1 ? parts[1].Trim() : "";

                txtMoladDate.Text = $"{englishPart}\n{hebrewPart}\n{moladDayOfWeek}, {gregorianMonthName} {gregorianDay}, {gregorianYear}";

                // Display the Molad time with chalakim (formatted by service)
                txtMoladJerusalemTime.Text = moladFormattedTime;
            }
            catch (Exception ex)
            {
                txtLocation.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }
    }
}

