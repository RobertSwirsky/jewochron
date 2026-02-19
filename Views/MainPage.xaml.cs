using Microsoft.UI.Xaml;
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
        private readonly YahrzeitService yahrzeitService;
        private DispatcherQueueTimer? clockTimer;
        private DispatcherQueueTimer? dataRefreshTimer;
        private DispatcherQueueTimer? camelTimer;
        private DispatcherQueueTimer? jewishManTimer;
        private DispatcherQueueTimer? camelMoveTimer;
        private DispatcherQueueTimer? jewishManMoveTimer;
        private readonly TimeZoneInfo jerusalemTimeZone;
        private DateTime lastRefreshDate = DateTime.MinValue;
        private string lastPrayerIndicator = "";
        private double currentMoonIllumination = 50.0; // Track moon phase for skyline

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

            // Initialize Yahrzeit service
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(appDataPath, "Jewochron", "yahrzeits.db");
            yahrzeitService = new YahrzeitService(dbPath, hebrewCalendarService);

            // Get Jerusalem time zone
            jerusalemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

            // Hook up Loaded event to ensure initial visual state is applied
            this.Loaded += MainPage_Loaded;

            // Start timers
            StartClockTimer();
            StartDataRefreshTimer();
            StartCamelAnimation();
            StartJewishManAnimation();

            // Load data
            _ = LoadDataAsync();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Apply initial visual state immediately without transitions
            // This ensures the layout is correct on first display
            var rootGrid = this.FindName("RootGrid") as Grid;
            if (rootGrid != null)
            {
                // Get actual dimensions
                double width = rootGrid.ActualWidth;
                double height = rootGrid.ActualHeight;

                System.Diagnostics.Debug.WriteLine($"[LAYOUT] Initial load: {width}x{height}");

                // If dimensions are not yet available, try to get them from XamlRoot
                if (width == 0 || height == 0 && this.XamlRoot != null)
                {
                    width = this.XamlRoot.Size.Width - 20; // Account for padding
                    height = this.XamlRoot.Size.Height - 20;
                    System.Diagnostics.Debug.WriteLine($"[LAYOUT] Using XamlRoot size: {width}x{height}");
                }

                // Apply visual state without transitions for immediate effect
                if (width > 0 && height > 0)
                {
                    UpdateVisualState(width, height, useTransitions: false);
                }
            }
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisualState(e.NewSize.Width, e.NewSize.Height, useTransitions: true);
        }

        private void UpdateVisualState(double width, double height, bool useTransitions = true)
        {
            // Avoid division by zero or invalid dimensions
            if (height == 0 || width == 0) return;

            double aspectRatio = width / height;

            string targetState;

            if (height > width)
            {
                // Portrait mode: Height > Width (e.g., 9:16 = 0.5625 ratio)
                // Use 2 columns if width allows, otherwise 1 column
                if (width >= 600)
                {
                    targetState = "PortraitWideState"; // 2 columns
                    System.Diagnostics.Debug.WriteLine($"[LAYOUT] Portrait Wide mode (2 columns): {width}x{height}");
                }
                else
                {
                    targetState = "PortraitNarrowState"; // 1 column
                    System.Diagnostics.Debug.WriteLine($"[LAYOUT] Portrait Narrow mode (1 column): {width}x{height}");
                }
            }
            else if (aspectRatio < 1.6 || width < 900)
            {
                // Landscape but narrow (e.g., 4:3 = 1.33, square = 1.0) or very small width
                // THREE COLUMN layout (changed from 2-column for consistency)
                targetState = "LandscapeNarrowState";
                System.Diagnostics.Debug.WriteLine($"[LAYOUT] Landscape Narrow (3 columns): {width}x{height}");
            }
            else if (width < 1600)
            {
                // Standard landscape 16:9 at moderate size
                // THREE COLUMN layout - good use of horizontal space
                // Good for 1920×1080 and similar displays
                targetState = "LandscapeWideState";
                System.Diagnostics.Debug.WriteLine($"[LAYOUT] Landscape Wide (3 columns): {width}x{height}");
            }
            else if (width < 2400)
            {
                // Extra wide displays
                // THREE COLUMN layout with larger cards
                // Good for 2560×1440, 2560×1080 (ultra-wide)
                targetState = "LandscapeExtraWideState";
                System.Diagnostics.Debug.WriteLine($"[LAYOUT] Landscape Extra Wide (3 columns, large): {width}x{height}");
            }
            else
            {
                // Ultra wide displays
                // FOUR COLUMN layout
                // Good for 3840×2160 (4K), 3440×1440 (ultra-wide), 5120×2880 (5K)
                targetState = "LandscapeUltraWideState";
                System.Diagnostics.Debug.WriteLine($"[LAYOUT] Landscape Ultra Wide (4 columns): {width}x{height}");
            }

            VisualStateManager.GoToState(this, targetState, useTransitions);
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
                // Stop any existing animation timer to prevent overlapping animations
                if (camelMoveTimer != null && camelMoveTimer.IsRunning)
                {
                    camelMoveTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Stopped previous animation timer");
                }

                // Reset to start position
                camelTransform.X = 0;
                animatedCamel.Opacity = 1;  // FULLY VISIBLE

                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Camel is now VISIBLE at opacity 1");
                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Look at the skyline NOW - you should see it!");

                // Create a simple timer to move it - slower, more majestic pace
                camelMoveTimer = DispatcherQueue.CreateTimer();
                camelMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                camelMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX -= 2;  // Move 2 pixels left each tick (slower than before)
                        camelTransform.X = currentX;

                        // After it goes off screen, stop and reset
                        if (currentX < -1300)
                        {
                            camelMoveTimer?.Stop();
                            animatedCamel.Opacity = 0;
                            camelTransform.X = 0;
                            System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Animation complete!");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CAMEL DEBUG] Tick error: {ex.Message}");
                        camelMoveTimer?.Stop();
                    }
                };

                camelMoveTimer.Start();
                System.Diagnostics.Debug.WriteLine("[CAMEL DEBUG] Timer animation started!");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CAMEL DEBUG] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
        }

        private void StartJewishManAnimation()
        {
            try
            {
                // Jewish man walks every 3 minutes (offset from camel)
                jewishManTimer = DispatcherQueue.CreateTimer();
                jewishManTimer.Interval = TimeSpan.FromMinutes(3);
                jewishManTimer.Tick += (s, e) => 
                {
                    try
                    {
                        AnimateJewishManWalk();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Jewish man animation error: {ex.Message}");
                    }
                };
                jewishManTimer.Start();

                // Start first animation after 30 seconds (offset from camel)
                var initialTimer = DispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(30);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Starting INITIAL Jewish man animation NOW!");
                        AnimateJewishManWalk();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Initial Jewish man animation error: {ex.Message}");
                    }
                };
                initialTimer.Start();
                System.Diagnostics.Debug.WriteLine("Jewish man animation timer initialized - first animation in 30 seconds");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start Jewish man animation: {ex.Message}");
            }
        }

        private void AnimateJewishManWalk()
        {
            // Get references to the Jewish man elements
            var animatedMan = this.FindName("AnimatedJewishMan") as Microsoft.UI.Xaml.UIElement;
            var manTransform = this.FindName("JewishManTransform") as Microsoft.UI.Xaml.Media.TranslateTransform;

            System.Diagnostics.Debug.WriteLine("========================================");
            System.Diagnostics.Debug.WriteLine($"[JEWISH MAN DEBUG] AnimateJewishManWalk called at {DateTime.Now:HH:mm:ss}");

            if (animatedMan == null || manTransform == null)
            {
                System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] ERROR: Elements not found!");
                return;
            }

            System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] Starting animation");

            try
            {
                // Stop any existing animation timer to prevent overlapping animations
                if (jewishManMoveTimer != null && jewishManMoveTimer.IsRunning)
                {
                    jewishManMoveTimer.Stop();
                    System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] Stopped previous animation timer");
                }

                // Reset to start position (left side)
                manTransform.X = 0;
                animatedMan.Opacity = 1;  // FULLY VISIBLE

                System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] Jewish man is now VISIBLE at opacity 1");

                // Create a simple timer to move him LEFT to RIGHT (opposite of camel)
                jewishManMoveTimer = DispatcherQueue.CreateTimer();
                jewishManMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                jewishManMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX += 2;  // Move 2 pixels RIGHT each tick
                        manTransform.X = currentX;

                        // After he goes off screen, stop and reset
                        if (currentX > 1250)
                        {
                            jewishManMoveTimer?.Stop();
                            animatedMan.Opacity = 0;
                            manTransform.X = 0;
                            System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] Animation complete!");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JEWISH MAN DEBUG] Tick error: {ex.Message}");
                        jewishManMoveTimer?.Stop();
                    }
                };

                jewishManMoveTimer.Start();
                System.Diagnostics.Debug.WriteLine("[JEWISH MAN DEBUG] Timer animation started!");
                System.Diagnostics.Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JEWISH MAN DEBUG] ERROR: {ex.Message}");
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
                // Position range: 150 (left) to 1050 (right) - keep within visible buildings area
                double sunPosition = 150 + ((timeOfDay - 6) / 12.0) * 900; // 6am to 6pm
                double moonPosition = 150 + ((timeOfDay + 12) % 24 / 12.0) * 900; // Opposite of sun

                // Update sun position (clamped to visible range above buildings)
                double sunLeft = Math.Clamp(sunPosition, 150, 1050);

                // Calculate sun height (arc across sky) - keep it in the sky area (above buildings)
                double sunArc = Math.Sin((timeOfDay - 6) / 12.0 * Math.PI); // 0 at dawn/dusk, 1 at noon
                double sunTop = 60 - (sunArc * 45); // Higher at noon, lower at dawn/dusk
                sunTop = Math.Clamp(sunTop, 5, 60);

                // Set sun canvas position
                Canvas.SetLeft(sunCanvas, sunLeft);
                Canvas.SetTop(sunCanvas, sunTop);

                // Update moon position - keep it HIGH in sky and within bounds
                double moonLeft = Math.Clamp(moonPosition, 150, 1050);
                double moonArc = Math.Sin(((timeOfDay + 12) % 24) / 12.0 * Math.PI);
                double moonTop = 35 - (moonArc * 25); // Much higher position overall
                moonTop = Math.Clamp(moonTop, 5, 35); // Clamp to stay high in sky area

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

                // Update moon phase appearance when moon is visible
                if (moonCanvas.Visibility == Visibility.Visible)
                {
                    UpdateSkylineMoonPhase(currentMoonIllumination);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skyline update error: {ex.Message}");
                // Silently ignore skyline update errors to prevent app crashes
                // The clock will continue to work even if skyline animation fails
            }
        }

        private void UpdateSkylineMoonPhase(double illuminationPercent)
        {
            try
            {
                var moonShadow = this.FindName("SkylineMoonShadow") as Microsoft.UI.Xaml.Shapes.Ellipse;
                var moonGlow = this.FindName("SkylineMoonGlow") as Microsoft.UI.Xaml.Shapes.Ellipse;
                var moonLit = this.FindName("SkylineMoonLit") as Microsoft.UI.Xaml.Shapes.Ellipse;

                if (moonShadow == null || moonLit == null) return;

                // Calculate moon age to determine if waxing or waning
                // Reference: Jan 6, 2000 at 18:14 was a known new moon
                double moonAge = (DateTime.Now - new DateTime(2000, 1, 6, 18, 14, 0)).TotalDays % 29.53;
                bool isWaxing = moonAge < 14.765; // First half of lunar cycle

                // The shadow is positioned to reveal the illuminated portion
                // illuminationPercent: 0 = new moon (all shadow), 100 = full moon (no shadow)
                // 
                // For waxing moon: right side lights up first, shadow slides LEFT off the moon
                // For waning moon: left side stays lit, shadow slides RIGHT onto the moon
                //
                // Shadow position: -40 (fully off left) to 0 (centered) to +40 (fully off right)

                // Calculate shadow offset based on illumination
                // At 0% illumination, shadow covers all (offset = 0)
                // At 100% illumination, shadow is completely off (offset = ±40)
                double shadowOffset = (illuminationPercent / 100.0) * 44;

                if (isWaxing)
                {
                    // Waxing: shadow slides left to reveal right side first
                    // Start: shadow at 0 (covering moon)
                    // End: shadow at -44 (off to the left)
                    Canvas.SetLeft(moonShadow, -shadowOffset);
                }
                else
                {
                    // Waning: shadow slides right to cover right side first  
                    // Start: shadow at 0 (covering moon from left)
                    // End: shadow at +44 (off to the right)
                    Canvas.SetLeft(moonShadow, shadowOffset - 44);
                }

                // Adjust glow intensity based on illumination
                if (moonGlow != null)
                {
                    moonGlow.Opacity = 0.08 + (illuminationPercent / 100.0 * 0.20);
                }

                // Slightly adjust lit portion brightness based on illumination
                moonLit.Opacity = 0.85 + (illuminationPercent / 100.0 * 0.15);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Moon phase update error: {ex.Message}");
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
                currentMoonIllumination = moonIllumination; // Store for skyline moon rendering
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
                string dateDisplay;

                if (isTwoDayRoshChodesh)
                {
                    // Two-day Rosh Chodesh: show both dates
                    DateTime firstDay = moladJerusalemTime;
                    DateTime secondDay = moladJerusalemTime.AddDays(1);

                    string firstMonthName = firstDay.ToString("MMMM");
                    int firstDayNum = firstDay.Day;

                    string secondMonthName = secondDay.ToString("MMMM");
                    int secondDayNum = secondDay.Day;
                    int year = secondDay.Year; // Use second day's year (in case it crosses year boundary)

                    if (firstMonthName == secondMonthName)
                    {
                        // Same month: "September 17/18 2026"
                        dateDisplay = $"{moladDayOfWeek}, {firstMonthName} {firstDayNum}/{secondDayNum}, {year}";
                    }
                    else
                    {
                        // Different months: "September 30/October 1 2027"
                        dateDisplay = $"{moladDayOfWeek}, {firstMonthName} {firstDayNum}/{secondMonthName} {secondDayNum}, {year}";
                    }
                }
                else
                {
                    // Single-day Rosh Chodesh
                    string gregorianMonthName = moladJerusalemTime.ToString("MMMM");
                    int gregorianDay = moladJerusalemTime.Day;
                    int gregorianYear = moladJerusalemTime.Year;
                    dateDisplay = $"{moladDayOfWeek}, {gregorianMonthName} {gregorianDay}, {gregorianYear}";
                }

                // Split English and Hebrew on separate lines, no moon emojis
                // roshChodeshInfo format: "Rosh Chodesh Month • ראש חודש חודש"
                string[] parts = roshChodeshInfo.Split('•');
                string englishPart = parts[0].Trim();
                string hebrewPart = parts.Length > 1 ? parts[1].Trim() : "";

                txtMoladDate.Text = $"{englishPart}\n{hebrewPart}\n{dateDisplay}";

                // Display the Molad time with chalakim (formatted by service)
                txtMoladJerusalemTime.Text = moladFormattedTime;

                // Check for upcoming yahrzeits
                await LoadYahrzeitsAsync();
            }
            catch (Exception ex)
            {
                txtLocation.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }

        private async Task LoadYahrzeitsAsync()
        {
            try
            {
                // Check for yahrzeits in the next 8 days (including today)
                var upcomingYahrzeits = await yahrzeitService.GetUpcomingYahrzeitsAsync(8);

                System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] Found {upcomingYahrzeits.Count} upcoming yahrzeit(s)");

                // Find the yahrzeit card container in the XAML
                var yahrzeitCard = this.FindName("YahrzeitCard") as Microsoft.UI.Xaml.UIElement;
                var yahrzeitPanel = this.FindName("YahrzeitPanel") as Microsoft.UI.Xaml.Controls.StackPanel;

                if (yahrzeitCard == null || yahrzeitPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("[YAHRZEIT] ERROR: Yahrzeit card elements not found in XAML");
                    return;
                }

                if (upcomingYahrzeits.Count == 0)
                {
                    // Hide the yahrzeit card if there are no upcoming yahrzeits
                    yahrzeitCard.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("[YAHRZEIT] No upcoming yahrzeits - card hidden");
                }
                else
                {
                    // Show the memorial plaque card and populate it
                    yahrzeitCard.Visibility = Visibility.Visible;
                    yahrzeitPanel.Children.Clear();
                    System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] Displaying {upcomingYahrzeits.Count} yahrzeit(s) on memorial plaque");

                    foreach (var upcoming in upcomingYahrzeits)
                    {
                        System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] - {upcoming.Yahrzeit.NameEnglish} ({upcoming.DaysFromNow} day{(upcoming.DaysFromNow == 1 ? "" : "s")} away)");

                        // Get Hebrew date with Hebrew numerals
                        string hebrewDay = hebrewCalendarService.ConvertToHebrewNumber(upcoming.HebrewDay);
                        string hebrewMonthName = hebrewCalendarService.GetHebrewMonthNameInHebrew(upcoming.HebrewMonth, 
                            hebrewCalendarService.GetHebrewDate(upcoming.Date).isLeapYear);
                        string hebrewYear = hebrewCalendarService.ConvertToHebrewNumber(upcoming.HebrewYear);
                        string hebrewDate = $"{hebrewDay} {hebrewMonthName} {hebrewYear}";

                        // Get appropriate honorific
                        string honorific = yahrzeitService.GetHonorific(upcoming.Yahrzeit.Gender);

                        // Create entry container (horizontal layout for plaque style)
                        var entryPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                        {
                            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                            Spacing = 4
                        };

                        // Name in English and Hebrew with honorific - dark text for bronze plaque
                        var nameText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = 22,
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 26)), // Dark brown/black
                            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
                        };
                        nameText.Text = $"{upcoming.Yahrzeit.NameEnglish} • {upcoming.Yahrzeit.NameHebrew} {honorific}";

                        // Hebrew date
                        var dateText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = 18,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.ColorHelper.FromArgb(255, 64, 32, 16)), // Dark bronze
                            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                            FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
                        };
                        dateText.Text = hebrewDate;

                        // When text (Today, or X days away)
                        var whenText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = 16,
                            FontWeight = upcoming.DaysFromNow == 0 ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                upcoming.DaysFromNow == 0 
                                    ? Microsoft.UI.ColorHelper.FromArgb(255, 139, 0, 0) // Dark red for TODAY
                                    : Microsoft.UI.ColorHelper.FromArgb(200, 64, 32, 16)), // Bronze
                            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
                        };
                        whenText.Text = upcoming.DaysFromNow == 0 
                            ? "🕯️ TODAY - Light a candle 🕯️" 
                            : $"In {upcoming.DaysFromNow} day{(upcoming.DaysFromNow == 1 ? "" : "s")} ({upcoming.Date:dddd, MMM d})";

                        entryPanel.Children.Add(nameText);
                        entryPanel.Children.Add(dateText);
                        entryPanel.Children.Add(whenText);

                        yahrzeitPanel.Children.Add(entryPanel);

                        // Add separator line if there are more entries
                        if (upcoming != upcomingYahrzeits.Last())
                        {
                            var separator = new Microsoft.UI.Xaml.Shapes.Rectangle
                            {
                                Height = 2,
                                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                    Microsoft.UI.ColorHelper.FromArgb(100, 139, 69, 19)), // Saddle brown
                                Margin = new Microsoft.UI.Xaml.Thickness(50, 8, 50, 8),
                                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                            };
                            yahrzeitPanel.Children.Add(separator);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading yahrzeits: {ex.Message}");
            }
        }
    }
}

