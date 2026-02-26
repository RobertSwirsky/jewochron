using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Jewochron.Services;
using Jewochron.Helpers;
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
        private readonly SimchaService simchaService;
        private readonly ShabbatTimesService shabbatTimesService;
        private readonly SkylineAnimationHelper animationHelper;
        private DispatcherQueueTimer? clockTimer;
        private DispatcherQueueTimer? dataRefreshTimer;
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
            shabbatTimesService = new ShabbatTimesService(halachicTimesService, hebrewCalendarService);

            // Initialize Yahrzeit service
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(appDataPath, "Jewochron", "yahrzeits.db");
            yahrzeitService = new YahrzeitService(dbPath, hebrewCalendarService);

            // Initialize Simcha service
            string simchaDbPath = Path.Combine(appDataPath, "Jewochron", "simchas.db");
            simchaService = new SimchaService(simchaDbPath, hebrewCalendarService);

            // Initialize animation helper
            animationHelper = new SkylineAnimationHelper(this);

            // Get Jerusalem time zone
            jerusalemTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");

            // Hook up Loaded event to ensure initial visual state is applied
            this.Loaded += MainPage_Loaded;

            // Start timers
            StartClockTimer();
            StartDataRefreshTimer();
            animationHelper.StartAnimationTimers();

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

        private static string GetCurrentPrayerIndicator(DateTime now)
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
                    UpdateSkylineMoonPhase(currentMoonIllumination, timeOfDay);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Skyline update error: {ex.Message}");
                // Silently ignore skyline update errors to prevent app crashes
                // The clock will continue to work even if skyline animation fails
            }
        }

        private void UpdateSkylineMoonPhase(double illuminationPercent, double timeOfDay)
        {
            try
            {
                var moonPhaseShape = this.FindName("SkylineMoonPhaseShape") as Microsoft.UI.Xaml.Shapes.Path;
                var moonGlow = this.FindName("SkylineMoonGlow") as Microsoft.UI.Xaml.Shapes.Ellipse;
                var moonDisc = this.FindName("SkylineMoonDisc") as Microsoft.UI.Xaml.Shapes.Ellipse;

                if (moonPhaseShape == null || moonDisc == null) return;

                // Calculate moon age to determine if waxing or waning
                // Reference: Jan 6, 2000 at 18:14 UTC was a known new moon
                double moonAge = (DateTime.UtcNow - new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc)).TotalDays % 29.53;
                bool isWaxing = moonAge < 14.765; // First half of lunar cycle

                // Clamp and normalize illumination for smoother visuals
                double clampedIllumination = Math.Clamp(illuminationPercent, 0.0, 100.0);
                double illuminationFactor = clampedIllumination / 100.0; // 0 = new, 1 = full

                // Calculate time-of-day opacity factor (fainter during bright day, stronger at night)
                double timeOpacityFactor = 1.0;
                if (timeOfDay >= 7 && timeOfDay < 17)
                {
                    timeOpacityFactor = 0.3; // Bright day
                }
                else if (timeOfDay >= 17 && timeOfDay < 19)
                {
                    timeOpacityFactor = 0.3 + (timeOfDay - 17) / 2.0 * 0.7; // Evening fade-in
                }
                else if (timeOfDay >= 5 && timeOfDay < 7)
                {
                    timeOpacityFactor = 1.0 - (timeOfDay - 5) / 2.0 * 0.7; // Dawn fade-out
                }

                // Apply time-based opacity to moon disc
                moonDisc.Opacity = (0.5 + illuminationFactor * 0.5) * timeOpacityFactor;

                // Adjust glow intensity based on illumination
                if (moonGlow != null)
                {
                    moonGlow.Opacity = (0.05 + (illuminationFactor * 0.22)) * timeOpacityFactor;
                }

                // Create the phase shadow shape using proper geometry
                const double radius = 20.0; // Moon radius
                const double centerX = radius;
                const double centerY = radius;

                // Generate the path geometry for the shadow (dark portion)
                var geometry = CreateMoonPhaseGeometry(centerX, centerY, radius, illuminationFactor, isWaxing);
                moonPhaseShape.Data = geometry;

                // Shadow opacity: full at new moon, mostly opaque even near full moon for visibility
                double shadowOpacity = Math.Max(0.85, 1.0 - illuminationFactor * 0.2) * timeOpacityFactor;
                moonPhaseShape.Opacity = shadowOpacity;

                // Adjust shadow gradient center based on phase for 3D depth
                var shadowGradient = this.FindName("ShadowGradient") as Microsoft.UI.Xaml.Media.RadialGradientBrush;
                if (shadowGradient != null)
                {
                    // Move gradient origin to create depth effect
                    double gradientX = isWaxing ? 0.3 : 0.7;
                    shadowGradient.GradientOrigin = new Windows.Foundation.Point(gradientX, 0.4);
                }

                // Enhancement: Earthshine effect (visible on dark side during crescent phases)
                var earthshine = this.FindName("SkylineEarthshine") as Microsoft.UI.Xaml.Shapes.Ellipse;
                if (earthshine != null)
                {
                    // Earthshine is most visible during crescent phases (10-40% illumination)
                    double earthshineOpacity = 0;
                    if (illuminationFactor < 0.4)
                    {
                        // Peak at ~25% illumination (crescent)
                        earthshineOpacity = Math.Sin(illuminationFactor * Math.PI / 0.4) * 0.15;
                    }
                    earthshine.Opacity = earthshineOpacity * timeOpacityFactor;
                }

                // Enhancement: Dynamic crater visibility based on terminator position
                UpdateCraterVisibility(illuminationFactor, isWaxing, timeOpacityFactor);

                // Enhancement: Smooth transitions using animation (optional - can be enabled)
                AnimateMoonPhaseTransition(moonPhaseShape, shadowOpacity);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Moon phase update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates crater visibility based on moon phase
        /// Craters in shadow should be less visible
        /// </summary>
        private void UpdateCraterVisibility(double illuminationFactor, bool isWaxing, double timeOpacityFactor)
        {
            try
            {
                // Get all craters
                var craters = new[]
                {
                    (this.FindName("Crater1") as Microsoft.UI.Xaml.Shapes.Ellipse, 8.0),   // X position
                    (this.FindName("Crater2") as Microsoft.UI.Xaml.Shapes.Ellipse, 20.0),
                    (this.FindName("Crater3") as Microsoft.UI.Xaml.Shapes.Ellipse, 14.0),
                    (this.FindName("Crater4") as Microsoft.UI.Xaml.Shapes.Ellipse, 26.0),
                    (this.FindName("Crater5") as Microsoft.UI.Xaml.Shapes.Ellipse, 30.0),
                    (this.FindName("Crater6") as Microsoft.UI.Xaml.Shapes.Ellipse, 18.0),
                    (this.FindName("Crater7") as Microsoft.UI.Xaml.Shapes.Ellipse, 10.0),
                    (this.FindName("Crater8") as Microsoft.UI.Xaml.Shapes.Ellipse, 24.0)
                };

                const double moonCenterX = 20.0;
                const double moonRadius = 20.0;

                // Calculate terminator position using area-accurate cosine formula
                // This matches the geometry rendering and ensures craters are properly lit/shadowed
                double terminatorX;
                if (isWaxing)
                {
                    terminatorX = moonCenterX + moonRadius * Math.Cos(Math.PI * (1 - illuminationFactor));
                }
                else
                {
                    terminatorX = moonCenterX - moonRadius * Math.Cos(Math.PI * (1 - illuminationFactor));
                }

                foreach (var (crater, craterX) in craters)
                {
                    if (crater == null) continue;

                    // Determine if crater is in lit or shadow region
                    bool isInLight;
                    if (isWaxing)
                    {
                        isInLight = craterX > terminatorX;
                    }
                    else
                    {
                        isInLight = craterX < terminatorX;
                    }

                    // Base opacity from XAML
                    double baseOpacity = crater.Opacity;

                    // Reduce opacity if in shadow, enhance if in light
                    double visibilityFactor = isInLight ? 1.0 : 0.3;
                    crater.Opacity = baseOpacity * visibilityFactor * timeOpacityFactor;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Crater visibility update error: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds smooth transition animation to moon phase changes
        /// </summary>
        private void AnimateMoonPhaseTransition(Microsoft.UI.Xaml.Shapes.Path phaseShape, double targetOpacity)
        {
            try
            {
                // Create subtle fade transition for smooth visual updates
                var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();

                var opacityAnimation = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
                {
                    To = targetOpacity,
                    Duration = new Duration(TimeSpan.FromMilliseconds(500)),
                    EasingFunction = new Microsoft.UI.Xaml.Media.Animation.QuadraticEase 
                    { 
                        EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseInOut 
                    }
                };

                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(opacityAnimation, phaseShape);
                Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

                storyboard.Children.Add(opacityAnimation);
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                // Animation is optional enhancement, fail silently
                System.Diagnostics.Debug.WriteLine($"Moon animation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates accurate moon phase geometry using the terminator curve
        /// </summary>
        private Microsoft.UI.Xaml.Media.Geometry CreateMoonPhaseGeometry(double centerX, double centerY, double radius, double phase, bool isWaxing)
        {
            // phase: 0 = new moon (all shadow), 1 = full moon (no shadow)

            // For phase = 0 (new moon): full circle of shadow
            // For phase = 0.5 (quarter): half circle (semicircle)
            // For phase = 1 (full moon): no shadow (empty geometry)

            if (phase >= 0.98)
            {
                // Full moon: return empty geometry (no shadow)
                return new Microsoft.UI.Xaml.Media.PathGeometry();
            }

            var pathFigure = new Microsoft.UI.Xaml.Media.PathFigure();

            if (phase <= 0.02)
            {
                // New moon: full circle of shadow
                pathFigure.StartPoint = new Windows.Foundation.Point(centerX - radius, centerY);

                var arc1 = new Microsoft.UI.Xaml.Media.ArcSegment
                {
                    Point = new Windows.Foundation.Point(centerX + radius, centerY),
                    Size = new Windows.Foundation.Size(radius, radius),
                    SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise,
                    IsLargeArc = false
                };

                var arc2 = new Microsoft.UI.Xaml.Media.ArcSegment
                {
                    Point = new Windows.Foundation.Point(centerX - radius, centerY),
                    Size = new Windows.Foundation.Size(radius, radius),
                    SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise,
                    IsLargeArc = false
                };

                pathFigure.Segments.Add(arc1);
                pathFigure.Segments.Add(arc2);
                pathFigure.IsClosed = true;
            }
            else
            {
                // Crescent, quarter, or gibbous: draw the dark portion
                // The terminator is an ellipse, the limb is a circle

                // Calculate the horizontal offset of the terminator from center
                // Using cosine function for area-accurate illumination mapping
                // This ensures the visible area matches the illumination percentage
                //
                // At phase 0: offset = -radius (new moon, 0% lit)
                // At phase 0.5: offset = 0 (quarter, 50% lit)
                // At phase 1: offset = +radius (full moon, 100% lit)
                //
                // Formula: cos(π * (1 - phase)) maps phase to offset with correct area relationship

                double terminatorOffset;
                bool shadowOnRight;

                if (isWaxing)
                {
                    // Waxing: shadow on left, retreating
                    // Use cosine formula for accurate area-to-illumination mapping
                    terminatorOffset = radius * Math.Cos(Math.PI * (1 - phase));
                    shadowOnRight = false;
                }
                else
                {
                    // Waning: shadow on right, advancing
                    terminatorOffset = -radius * Math.Cos(Math.PI * (1 - phase));
                    shadowOnRight = true;
                }

                // Draw the shadow region
                // It's bounded by the terminator ellipse and the limb arc

                if (shadowOnRight)
                {
                    // Shadow on right side (waning)
                    // Start at top of terminator
                    double terminatorX = centerX + terminatorOffset;
                    pathFigure.StartPoint = new Windows.Foundation.Point(terminatorX, centerY - radius);

                    // Arc along the right limb from top to bottom
                    var limbArc = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(centerX + radius, centerY),
                        Size = new Windows.Foundation.Size(radius, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(limbArc);

                    var limbArc2 = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(terminatorX, centerY + radius),
                        Size = new Windows.Foundation.Size(radius, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(limbArc2);

                    // Terminator curve back to start (elliptical arc)
                    double ellipseWidth = Math.Abs(radius - Math.Abs(terminatorOffset));
                    var terminatorArc = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(terminatorX, centerY - radius),
                        Size = new Windows.Foundation.Size(ellipseWidth, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Counterclockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(terminatorArc);
                }
                else
                {
                    // Shadow on left side (waxing)
                    double terminatorX = centerX + terminatorOffset;
                    pathFigure.StartPoint = new Windows.Foundation.Point(terminatorX, centerY - radius);

                    // Arc along the left limb from top to bottom
                    var limbArc = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(centerX - radius, centerY),
                        Size = new Windows.Foundation.Size(radius, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Counterclockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(limbArc);

                    var limbArc2 = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(terminatorX, centerY + radius),
                        Size = new Windows.Foundation.Size(radius, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Counterclockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(limbArc2);

                    // Terminator curve back to start
                    double ellipseWidth = Math.Abs(radius - Math.Abs(terminatorOffset));
                    var terminatorArc = new Microsoft.UI.Xaml.Media.ArcSegment
                    {
                        Point = new Windows.Foundation.Point(terminatorX, centerY - radius),
                        Size = new Windows.Foundation.Size(ellipseWidth, radius),
                        SweepDirection = Microsoft.UI.Xaml.Media.SweepDirection.Clockwise,
                        IsLargeArc = false
                    };
                    pathFigure.Segments.Add(terminatorArc);
                }

                pathFigure.IsClosed = true;
            }

            var pathGeometry = new Microsoft.UI.Xaml.Media.PathGeometry();
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
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
        
        private static string ConvertToHalachicTime(DateTime time, DateTime sunrise, DateTime sunset)
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
                txtLocationEnglish.Text = $"📍 {city}, {state}";

                // Only show Hebrew location if we have a translation
                string hebrewLocation = TranslateToHebrew(city, state);
                if (hebrewLocation != $"{city}, {state}")
                {
                    // We have a Hebrew translation
                    txtLocationHebrew.Text = $"📍 {hebrewLocation}";
                    txtLocationHebrew.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    // No translation available, hide Hebrew location
                    txtLocationHebrew.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }

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
                var (holidayEnglish, holidayHebrew, holidayDate, daysUntil, isFast, is24HourFast) = jewishHolidaysService.GetNextHoliday(now);
                txtNextHolidayEnglish.Text = holidayEnglish;
                txtNextHolidayHebrew.Text = holidayHebrew;
                txtDaysUntilHoliday.Text = daysUntil.ToString();

                // Get Hebrew date for the holiday
                var (holidayHebrewYear, holidayHebrewMonth, holidayHebrewDay, holidayIsLeapYear) = hebrewCalendarService.GetHebrewDate(holidayDate);
                string holidayHebrewMonthName = hebrewCalendarService.GetHebrewMonthNameInHebrew(holidayHebrewMonth, holidayIsLeapYear);
                string holidayHebrewDayStr = hebrewCalendarService.ConvertToHebrewNumber(holidayHebrewDay);
                string hebrewHolidayDate = $"{holidayHebrewDayStr} {holidayHebrewMonthName}";

                // Format Gregorian date
                string englishHolidayDate = holidayDate.ToString("MMMM d");

                // Display both Hebrew and English dates
                txtHolidayDate.Text = $"{hebrewHolidayDate} • {englishHolidayDate}";

                // Add day of week
                var holidayDayOfWeek = this.FindName("txtHolidayDayOfWeek") as Microsoft.UI.Xaml.Controls.TextBlock;
                if (holidayDayOfWeek != null)
                {
                    holidayDayOfWeek.Text = holidayDate.ToString("dddd");
                }

                // Display fast times if this is a fast day
                var txtFastTimes = this.FindName("txtFastTimes") as Microsoft.UI.Xaml.Controls.TextBlock;
                if (txtFastTimes != null)
                {
                    if (isFast)
                    {
                        var holidayTimes = halachicTimesService.CalculateTimes(holidayDate, latitude, longitude);

                        if (is24HourFast)
                        {
                            // 24-hour fast: sunset to nightfall (Yom Kippur, Tisha B'Av)
                            DateTime fastStart = holidayTimes.sunset.AddDays(-1); // Previous evening
                            DateTime fastEnd = holidayTimes.tzait; // Nightfall (tzait)

                            txtFastTimes.Text = $"⏰ Fast: {fastStart:dddd h:mm tt} - {fastEnd:dddd h:mm tt}";
                            txtFastTimes.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        }
                        else
                        {
                            // Dawn-to-dusk fast: alot hashachar to nightfall
                            DateTime fastStart = holidayTimes.alotHaShachar; // Dawn
                            DateTime fastEnd = holidayTimes.tzait; // Nightfall

                            txtFastTimes.Text = $"⏰ Fast: {fastStart:h:mm tt} - {fastEnd:h:mm tt}";
                            txtFastTimes.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                        }
                    }
                    else
                    {
                        txtFastTimes.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                    }
                }

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

                // Next Shabbat times
                var (candleLighting, havdalah, shabbatDate, parshaName) = shabbatTimesService.GetNextShabbatTimes(now, latitude, longitude);

                // Get Hebrew date for Shabbat (Saturday)
                var (shabbatHebrewYear, shabbatHebrewMonth, shabbatHebrewDay, shabbatIsLeapYear) = hebrewCalendarService.GetHebrewDate(shabbatDate);

                // Format the date in English
                string englishShabbatDate = shabbatDate.ToString("MMMM d");

                // Format the date in Hebrew
                string shabbatHebrewMonthName = hebrewCalendarService.GetHebrewMonthNameInHebrew(shabbatHebrewMonth, shabbatIsLeapYear);
                string shabbatHebrewDayStr = hebrewCalendarService.ConvertToHebrewNumber(shabbatHebrewDay);
                string hebrewShabbatDate = $"{shabbatHebrewDayStr} {shabbatHebrewMonthName}";

                // Display both Hebrew and English dates (no "Shabbat" prefix to avoid repetition with card title)
                txtShabbatDate.Text = $"{hebrewShabbatDate} • {englishShabbatDate}";

                // Format the times
                // Show that candle lighting is Friday evening (the start of Shabbat)
                DateTime fridayDate = candleLighting.Date;
                string candleLightingDay = fridayDate.DayOfWeek.ToString();
                txtCandleLighting.Text = $"{candleLightingDay} {candleLighting:h:mm tt}";
                txtHavdalah.Text = havdalah.ToString("h:mm tt");

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

                // Check for upcoming yahrzeits and simchas
                await LoadYahrzeitsAsync();
                await LoadSimchasAsync();
            }
            catch (Exception ex)
            {
                txtLocationEnglish.Text = $"Error: {ex.Message}";
                txtEnglishDate.Text = "Error loading data";
            }
        }

        public async Task RefreshYahrzeitsAsync()
        {
            System.Diagnostics.Debug.WriteLine("[YAHRZEIT] RefreshYahrzeitsAsync called");
            await LoadYahrzeitsAsync();
            await LoadSimchasAsync();
        }

        private async Task LoadYahrzeitsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[YAHRZEIT] LoadYahrzeitsAsync called");
                // Check for yahrzeits in the next 8 days (including today)
                var upcomingYahrzeits = await yahrzeitService.GetUpcomingYahrzeitsAsync(8);

                System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] Found {upcomingYahrzeits.Count} upcoming yahrzeit(s)");

                // Find the yahrzeit card container in the XAML
                var yahrzeitCard = this.FindName("YahrzeitCard") as Microsoft.UI.Xaml.UIElement;
                var yahrzeitPanel = this.FindName("YahrzeitPanel") as Microsoft.UI.Xaml.Controls.StackPanel;
                var yahrzeitTitle = this.FindName("txtYahrzeitTitle") as Microsoft.UI.Xaml.Controls.TextBlock;

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
                    // Check if any yahrzeit is TODAY
                    bool hasYahrzeitToday = upcomingYahrzeits.Any(y => y.DaysFromNow == 0);

                    // Update title based on whether it's today
                    if (yahrzeitTitle != null)
                    {
                        yahrzeitTitle.Text = hasYahrzeitToday 
                            ? "🕯️ TODAY IS A YAHRZEIT • היום יארצייט 🕯️"
                            : "🕯️ In Loving Memory • לזכר נשמת 🕯️";
                    }

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

                        // Create entry container
                        var entryPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                        {
                            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                            Spacing = 4
                        };

                        // If yahrzeit is TODAY, add animated flame
                        if (upcoming.DaysFromNow == 0)
                        {
                            var flamePanel = CreateAnimatedFlamePanel();
                            entryPanel.Children.Add(flamePanel);
                        }

                        // Name in English and Hebrew with honorific - dark text for bronze plaque
                        var nameText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = upcoming.DaysFromNow == 0 ? 24 : 22,
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
                            FontSize = upcoming.DaysFromNow == 0 ? 20 : 18,
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
                            FontSize = upcoming.DaysFromNow == 0 ? 18 : 16,
                            FontWeight = upcoming.DaysFromNow == 0 ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                upcoming.DaysFromNow == 0 
                                    ? Microsoft.UI.ColorHelper.FromArgb(255, 139, 0, 0) // Dark red for TODAY
                                    : Microsoft.UI.ColorHelper.FromArgb(200, 64, 32, 16)), // Bronze
                            TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center
                        };
                        whenText.Text = upcoming.DaysFromNow == 0 
                            ? "TODAY - Please light a yahrzeit candle" 
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

                    // Start flame animation if there's a yahrzeit today
                    if (hasYahrzeitToday)
                    {
                        StartFlameAnimation();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading yahrzeits: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads upcoming simchas (within next 8 days or later in current month)
        /// </summary>
        private async Task LoadSimchasAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[SIMCHAS] LoadSimchasAsync called");

                // Get all simchas
                var allSimchas = await simchaService.GetAllSimchasAsync();
                var today = DateTime.Today;
                var currentHebrewDate = hebrewCalendarService.GetHebrewDate(today);

                // Filter for upcoming simchas: within next 8 days OR later in current Hebrew month
                var upcomingSimchas = new List<(Models.Simcha Simcha, DateTime NextDate, int DaysFromNow, int HebrewDay, int HebrewMonth, int HebrewYear)>();

                foreach (var simcha in allSimchas)
                {
                    // Calculate next occurrence
                    var nextOccurrence = simcha.GetNextOccurrence(hebrewCalendarService);

                    if (nextOccurrence.HasValue)
                    {
                        int daysFromNow = (nextOccurrence.Value.Date - today).Days;
                        var nextHebrewDate = hebrewCalendarService.GetHebrewDate(nextOccurrence.Value);

                        // Include if within 8 days OR later in current Hebrew month
                        bool isWithin8Days = daysFromNow >= 0 && daysFromNow <= 7;
                        bool isLaterThisMonth = nextHebrewDate.month == currentHebrewDate.month && 
                                                nextHebrewDate.year == currentHebrewDate.year &&
                                                daysFromNow > 0;

                        if (isWithin8Days || isLaterThisMonth)
                        {
                            upcomingSimchas.Add((simcha, nextOccurrence.Value, daysFromNow, 
                                nextHebrewDate.day, nextHebrewDate.month, nextHebrewDate.year));
                        }
                    }
                }

                // Sort by days from now
                upcomingSimchas = upcomingSimchas.OrderBy(s => s.DaysFromNow).ToList();

                System.Diagnostics.Debug.WriteLine($"[SIMCHAS] Found {upcomingSimchas.Count} upcoming simcha(s)");

                // Find the simchas card container in the XAML
                var simchasCard = this.FindName("SimchasCard") as Microsoft.UI.Xaml.UIElement;
                var simchasPanel = this.FindName("SimchasPanel") as Microsoft.UI.Xaml.Controls.StackPanel;

                if (simchasCard == null || simchasPanel == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SIMCHAS] ERROR: Simchas card elements not found in XAML");
                    return;
                }

                if (upcomingSimchas.Count == 0)
                {
                    // Hide the simchas card if there are no upcoming simchas
                    simchasCard.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("[SIMCHAS] No upcoming simchas - card hidden");
                }
                else
                {
                    // Show the simchas card and populate it
                    simchasCard.Visibility = Visibility.Visible;
                    simchasPanel.Children.Clear();
                    System.Diagnostics.Debug.WriteLine($"[SIMCHAS] Displaying {upcomingSimchas.Count} simcha(s)");

                    foreach (var upcoming in upcomingSimchas)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SIMCHAS] - {upcoming.Simcha.Name} ({upcoming.DaysFromNow} day(s) away)");

                        // Get Hebrew date with Hebrew numerals
                        string hebrewDay = hebrewCalendarService.ConvertToHebrewNumber(upcoming.HebrewDay);
                        string hebrewMonthName = hebrewCalendarService.GetHebrewMonthNameInHebrew(upcoming.HebrewMonth, 
                            hebrewCalendarService.GetHebrewDate(upcoming.NextDate).isLeapYear);
                        string hebrewYear = hebrewCalendarService.ConvertToHebrewNumber(upcoming.HebrewYear);
                        string hebrewDate = $"{hebrewDay} {hebrewMonthName} {hebrewYear}";

                        // Get icon for simcha type
                        string icon = GetSimchaIcon(upcoming.Simcha.Type);

                        // Create entry container
                        var entryPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                        {
                            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
                            Spacing = 12,
                            Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
                        };

                        // Icon
                        var iconText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            Text = icon,
                            FontSize = 32,
                            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center
                        };

                        // Details panel
                        var detailsPanel = new Microsoft.UI.Xaml.Controls.StackPanel
                        {
                            Orientation = Microsoft.UI.Xaml.Controls.Orientation.Vertical,
                            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center,
                            Spacing = 2
                        };

                        // Name and type
                        var nameText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = upcoming.DaysFromNow == 0 ? 22 : 20,
                            FontWeight = upcoming.DaysFromNow == 0 ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.SemiBold,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.ColorHelper.FromArgb(255, 26, 26, 26)),
                            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
                        };
                        nameText.Text = $"{upcoming.Simcha.Name} • {upcoming.Simcha.Type}";

                        // Hebrew date
                        var dateText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = 16,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                Microsoft.UI.ColorHelper.FromArgb(255, 64, 32, 64)),
                            FlowDirection = Microsoft.UI.Xaml.FlowDirection.RightToLeft
                        };
                        dateText.Text = hebrewDate;

                        // When text (Today, Tomorrow, or X days away)
                        var whenText = new Microsoft.UI.Xaml.Controls.TextBlock
                        {
                            FontSize = upcoming.DaysFromNow == 0 ? 16 : 14,
                            FontWeight = upcoming.DaysFromNow == 0 ? Microsoft.UI.Text.FontWeights.Bold : Microsoft.UI.Text.FontWeights.Normal,
                            Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                upcoming.DaysFromNow == 0 
                                    ? Microsoft.UI.ColorHelper.FromArgb(255, 255, 20, 147) // Hot pink for TODAY
                                    : Microsoft.UI.ColorHelper.FromArgb(200, 139, 0, 139)) // Dark magenta
                        };

                        if (upcoming.DaysFromNow == 0)
                            whenText.Text = "🎊 TODAY! 🎊";
                        else if (upcoming.DaysFromNow == 1)
                            whenText.Text = "Tomorrow";
                        else
                            whenText.Text = $"In {upcoming.DaysFromNow} days ({upcoming.NextDate:dddd, MMM d})";

                        detailsPanel.Children.Add(nameText);
                        detailsPanel.Children.Add(dateText);
                        detailsPanel.Children.Add(whenText);

                        entryPanel.Children.Add(iconText);
                        entryPanel.Children.Add(detailsPanel);

                        simchasPanel.Children.Add(entryPanel);

                        // Add separator line if there are more entries
                        if (upcoming != upcomingSimchas.Last())
                        {
                            var separator = new Microsoft.UI.Xaml.Shapes.Rectangle
                            {
                                Height = 1,
                                Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                                    Microsoft.UI.ColorHelper.FromArgb(80, 0, 0, 0)),
                                Margin = new Microsoft.UI.Xaml.Thickness(0, 8, 0, 8)
                            };
                            simchasPanel.Children.Add(separator);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading simchas: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the appropriate icon for a simcha type
        /// </summary>
        private string GetSimchaIcon(string type)
        {
            return type switch
            {
                "Hebrew Birthday" => "🎂",
                "Bar Mitzvah" => "📜",
                "Bat Mitzvah" => "📜",
                "Wedding" => "💒",
                "Engagement" => "💍",
                "Brit Milah" => "👶",
                "Pidyon HaBen" => "🕊️",
                "Upsherin" => "✂️",
                "Anniversary" => "💝",
                _ => "🎉"
            };
        }

        /// <summary>
        /// Creates an animated flame panel for yahrzeit candle
        /// </summary>
        private static Microsoft.UI.Xaml.Controls.StackPanel CreateAnimatedFlamePanel()
        {
            var flamePanel = new Microsoft.UI.Xaml.Controls.StackPanel
            {
                Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal,
                HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center,
                Spacing = 8,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 8)
            };

            // Create three flame emojis that will be animated
            for (int i = 0; i < 3; i++)
            {
                var flame = new Microsoft.UI.Xaml.Controls.TextBlock
                {
                    Text = "🕯️",
                    FontSize = 32,
                    Name = $"YahrzeitFlame_{i}",
                    RenderTransform = new Microsoft.UI.Xaml.Media.CompositeTransform()
                };
                flamePanel.Children.Add(flame);
            }

            return flamePanel;
        }

        private DispatcherQueueTimer? _flameAnimationTimer;
        private double _flameAnimationPhase = 0;

        /// <summary>
        /// Starts the flickering flame animation for yahrzeit candles
        /// </summary>
        private void StartFlameAnimation()
        {
            // Stop any existing animation
            _flameAnimationTimer?.Stop();

            _flameAnimationTimer = DispatcherQueue.CreateTimer();
            _flameAnimationTimer.Interval = TimeSpan.FromMilliseconds(100);
            _flameAnimationTimer.Tick += (s, e) =>
            {
                try
                {
                    _flameAnimationPhase += 0.3;

                    // Find all flame elements and animate them
                    var yahrzeitPanel = this.FindName("YahrzeitPanel") as Microsoft.UI.Xaml.Controls.StackPanel;
                    if (yahrzeitPanel == null) return;

                    AnimateFlamesInPanel(yahrzeitPanel);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Flame animation error: {ex.Message}");
                }
            };
            _flameAnimationTimer.Start();
        }

        private void AnimateFlamesInPanel(Microsoft.UI.Xaml.Controls.StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Microsoft.UI.Xaml.Controls.StackPanel childPanel)
                {
                    // Check for flame panels (horizontal with candle emojis)
                    if (childPanel.Orientation == Microsoft.UI.Xaml.Controls.Orientation.Horizontal)
                    {
                        int flameIndex = 0;
                        foreach (var flameChild in childPanel.Children)
                        {
                            if (flameChild is Microsoft.UI.Xaml.Controls.TextBlock flameText && 
                                flameText.Text == "🕯️" &&
                                flameText.RenderTransform is Microsoft.UI.Xaml.Media.CompositeTransform transform)
                            {
                                // Create gentle flickering effect with different phases for each flame
                                double phase = _flameAnimationPhase + (flameIndex * 1.2);
                                double scaleX = 1.0 + Math.Sin(phase) * 0.05;
                                double scaleY = 1.0 + Math.Sin(phase * 1.3) * 0.08;
                                double rotation = Math.Sin(phase * 0.7) * 3;

                                transform.ScaleX = scaleX;
                                transform.ScaleY = scaleY;
                                transform.Rotation = rotation;
                                transform.CenterX = 16;
                                transform.CenterY = 32;

                                flameIndex++;
                            }
                        }
                    }
                    else
                    {
                        // Recurse into vertical panels
                        AnimateFlamesInPanel(childPanel);
                    }
                }
            }
        }

        private string TranslateToHebrew(string city, string state)
        {
            // Dictionary of common US cities and states in Hebrew
            var cityTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Major cities
                {"New York", "ניו יורק"},
                {"Los Angeles", "לוס אנג'לס"},
                {"Chicago", "שיקגו"},
                {"Houston", "יוסטון"},
                {"Phoenix", "פיניקס"},
                {"Philadelphia", "פילדלפיה"},
                {"San Antonio", "סן אנטוניו"},
                {"San Diego", "סן דייגו"},
                {"Dallas", "דאלאס"},
                {"San Jose", "סן חוזה"},
                {"Austin", "אוסטין"},
                {"Jacksonville", "ג'קסונוויל"},
                {"Fort Worth", "פורט וורת'"},
                {"Columbus", "קולומבוס"},
                {"Charlotte", "שרלוט"},
                {"Indianapolis", "אינדיאנפוליס"},
                {"Seattle", "סיאטל"},
                {"Denver", "דנוור"},
                {"Boston", "בוסטון"},
                {"Detroit", "דטרויט"},
                {"Miami", "מיאמי"},
                {"Atlanta", "אטלנטה"},
                {"Washington", "וושינגטון"},
                {"Baltimore", "בולטימור"},
                {"Milwaukee", "מילווקי"},
                {"Las Vegas", "לאס וגאס"},
                {"Nashville", "נאשוויל"},
                {"Portland", "פורטלנד"},
                {"Memphis", "ממפיס"},
                {"Louisville", "לואיוויל"},
                {"Minneapolis", "מיניאפוליס"},
                {"Cleveland", "קליבלנד"},
                {"Orlando", "אורלנדו"},
                {"Tampa", "טמפה"},
                {"Pittsburgh", "פיטסבורג"},
                {"Cincinnati", "סינסינטי"},
                {"Kansas City", "קנזס סיטי"},
                {"St. Louis", "סנט לואיס"},
                {"Sacramento", "סקרמנטו"},
                {"San Francisco", "סן פרנסיסקו"},
                {"Sunnyvale", "סאניוויל"},
                {"Palo Alto", "פאלו אלטו"},
                {"Mountain View", "מאונטיין וויו"},
                {"Santa Clara", "סנטה קלרה"},
                {"Cupertino", "קופרטינו"},
                {"Fremont", "פרימונט"},
                {"Oakland", "אוקלנד"},
                {"Berkeley", "ברקלי"},
                {"Buffalo", "באפלו"},
                {"Rochester", "רוצ'סטר"},
                {"Lakewood", "לייקווד"},
                {"Passaic", "פסייק"},
                {"Monsey", "מונסי"},
                {"Teaneck", "טינק"},
                {"Bergenfield", "ברגנפילד"},
                {"Fair Lawn", "פייר לון"},
                {"Unknown", "לא ידוע"}
            };

            var stateTranslations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"NY", "ניו יורק"},
                {"CA", "קליפורניה"},
                {"TX", "טקסס"},
                {"FL", "פלורידה"},
                {"PA", "פנסילבניה"},
                {"IL", "אילינוי"},
                {"OH", "אוהיו"},
                {"GA", "ג'ורג'יה"},
                {"NC", "צפון קרוליינה"},
                {"MI", "מישיגן"},
                {"NJ", "ניו ג'רזי"},
                {"VA", "וירג'יניה"},
                {"WA", "וושינגטון"},
                {"AZ", "אריזונה"},
                {"MA", "מסצ'וסטס"},
                {"TN", "טנסי"},
                {"IN", "אינדיאנה"},
                {"MO", "מיזורי"},
                {"MD", "מרילנד"},
                {"WI", "ויסקונסין"},
                {"CO", "קולורדו"},
                {"MN", "מינסוטה"},
                {"SC", "דרום קרוליינה"},
                {"AL", "אלבמה"},
                {"LA", "לואיזיאנה"},
                {"KY", "קנטאקי"},
                {"OR", "אורגון"},
                {"OK", "אוקלהומה"},
                {"CT", "קונטיקט"},
                {"UT", "יוטה"},
                {"NV", "נבדה"},
                {"Unknown", "לא ידוע"}
            };

            bool hasCityTranslation = cityTranslations.TryGetValue(city, out var cityHebrew);
            bool hasStateTranslation = stateTranslations.TryGetValue(state, out var stateHebrew);

            // Only return Hebrew if BOTH city and state have translations
            if (hasCityTranslation && hasStateTranslation)
            {
                return $"{cityHebrew}, {stateHebrew}";
            }

            // Return original if either is missing - this will trigger the "no translation" logic
            return $"{city}, {state}";
        }
    }
}

