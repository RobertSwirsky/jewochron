using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Diagnostics;

namespace Jewochron.Helpers
{
    /// <summary>
    /// Handles Jerusalem skyline visual updates based on time of day
    /// </summary>
    public class SkylineUpdater
    {
        private readonly Page _page;
        private double _currentMoonIllumination = 50.0;

        public SkylineUpdater(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public double CurrentMoonIllumination
        {
            get => _currentMoonIllumination;
            set => _currentMoonIllumination = value;
        }

        public void UpdateSkyline(DateTime jerusalemTime)
        {
            try
            {
                // Check if skyline elements exist
                var sunCanvas = _page.FindName("SunCanvas") as UIElement;
                var moonCanvas = _page.FindName("MoonCanvas") as UIElement;
                var moonGlowCanvas = _page.FindName("MoonGlowCanvas") as UIElement;
                var starsCanvas = _page.FindName("StarsCanvas") as UIElement;
                var skyLayer1 = _page.FindName("SkyLayer1");
                var skyLayer2 = _page.FindName("SkyLayer2");
                var skyLayer3 = _page.FindName("SkyLayer3");

                if (sunCanvas == null || moonCanvas == null || skyLayer1 == null)
                {
                    Debug.WriteLine("Skyline elements not found - skipping update");
                    return;
                }

                int hour = jerusalemTime.Hour;
                int minute = jerusalemTime.Minute;
                double timeOfDay = hour + (minute / 60.0);

                // Calculate sun/moon positions
                double sunPosition = 100 + ((timeOfDay - 6) / 12.0) * 1000; // 6am to 6pm
                double moonPosition = 100 + ((timeOfDay + 12) % 24 / 12.0) * 1000;

                // Update sun position
                double sunLeft = Math.Clamp(sunPosition, 100, 1100);
                double sunArc = Math.Sin((timeOfDay - 6) / 12.0 * Math.PI);
                double sunTop = 80 - (sunArc * 50);
                sunTop = Math.Clamp(sunTop, 20, 80);

                Canvas.SetLeft(sunCanvas, sunLeft);
                Canvas.SetTop(sunCanvas, sunTop);

                // Update moon position - KEEP WITHIN SKYLINE BOUNDS
                double moonLeft = Math.Clamp(moonPosition, 100, 1100);
                double moonArc = Math.Sin(((timeOfDay + 12) % 24) / 12.0 * Math.PI);
                double moonTop = 60 - (moonArc * 50); // Keeps moon in visible area
                moonTop = Math.Clamp(moonTop, 10, 60);

                Canvas.SetLeft(moonCanvas, moonLeft);
                Canvas.SetTop(moonCanvas, moonTop);

                // Position glow canvas relative to moon (offset by -5 for larger glow)
                if (moonGlowCanvas != null)
                {
                    Canvas.SetLeft(moonGlowCanvas, moonLeft - 5);
                    Canvas.SetTop(moonGlowCanvas, moonTop - 5);
                }

                // Update moon phase appearance
                UpdateMoonPhase(_currentMoonIllumination);

                // Set sky colors and visibility based on time of day
                ApplyTimeOfDaySettings(timeOfDay, sunCanvas, moonCanvas, moonGlowCanvas, starsCanvas);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Skyline update error: {ex.Message}");
            }
        }

        private void ApplyTimeOfDaySettings(double timeOfDay, UIElement sunCanvas, UIElement moonCanvas, UIElement? moonGlowCanvas, UIElement? starsCanvas)
        {
            if (timeOfDay >= 5 && timeOfDay < 6) // Dawn (5am-6am)
            {
                SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.5, 0.4);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Visible;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Visible;
                if (starsCanvas != null)
                {
                    starsCanvas.Visibility = Visibility.Visible;
                    starsCanvas.Opacity = 1.0 - (timeOfDay - 5);
                }
            }
            else if (timeOfDay >= 6 && timeOfDay < 7) // Sunrise
            {
                SetSkyColors("#FF6B6B", "#FFA07A", "#FFD700", 0.6, 0.3);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Collapsed;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Collapsed;
                if (starsCanvas != null)
                    starsCanvas.Visibility = Visibility.Collapsed;
            }
            else if (timeOfDay >= 7 && timeOfDay < 10) // Morning
            {
                SetSkyColors("#87CEEB", "#B0E0E6", "#E0F6FF", 0.5, 0.3);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Collapsed;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Collapsed;
                if (starsCanvas != null)
                    starsCanvas.Visibility = Visibility.Collapsed;
            }
            else if (timeOfDay >= 10 && timeOfDay < 15) // Midday
            {
                SetSkyColors("#4A90E2", "#5DA3E8", "#87CEEB", 0.4, 0.2);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Collapsed;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Collapsed;
                if (starsCanvas != null)
                    starsCanvas.Visibility = Visibility.Collapsed;
            }
            else if (timeOfDay >= 15 && timeOfDay < 17) // Afternoon
            {
                SetSkyColors("#6BA4D8", "#87CEEB", "#B0E0E6", 0.5, 0.3);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Collapsed;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Collapsed;
                if (starsCanvas != null)
                    starsCanvas.Visibility = Visibility.Collapsed;
            }
            else if (timeOfDay >= 17 && timeOfDay < 18) // Late afternoon
            {
                SetSkyColors("#FF8C42", "#FFB366", "#FFD699", 0.6, 0.4);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Collapsed;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Collapsed;
                if (starsCanvas != null)
                    starsCanvas.Visibility = Visibility.Collapsed;
            }
            else if (timeOfDay >= 18 && timeOfDay < 19) // Sunset
            {
                SetSkyColors("#FF6B6B", "#FF8C69", "#FFB347", 0.7, 0.5);
                sunCanvas.Visibility = Visibility.Visible;
                moonCanvas.Visibility = Visibility.Visible;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Visible;
                if (starsCanvas != null)
                {
                    starsCanvas.Visibility = Visibility.Visible;
                    starsCanvas.Opacity = timeOfDay - 18;
                }
            }
            else if (timeOfDay >= 19 && timeOfDay < 20) // Dusk
            {
                SetSkyColors("#4A5568", "#5A6B7D", "#6B7C8F", 0.6, 0.4);
                sunCanvas.Visibility = Visibility.Collapsed;
                moonCanvas.Visibility = Visibility.Visible;
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Visible;
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
                if (moonGlowCanvas != null) moonGlowCanvas.Visibility = Visibility.Visible;
                if (starsCanvas != null)
                {
                    starsCanvas.Visibility = Visibility.Visible;
                    starsCanvas.Opacity = 1.0;
                }
            }
        }

        private void UpdateMoonPhase(double illuminationPercent)
        {
            try
            {
                var moonShadow = _page.FindName("SkylineMoonShadow") as Ellipse;
                var moonLit = _page.FindName("SkylineMoonLit") as Ellipse;
                var moonGlow = _page.FindName("SkylineMoonGlow") as Ellipse;

                if (moonShadow == null || moonLit == null)
                {
                    Debug.WriteLine("Moon phase elements not found (SkylineMoonShadow/SkylineMoonLit)");
                    return;
                }

                // Calculate moon age to determine if waxing or waning
                // Reference: Jan 6, 2000 at 18:14 was a known new moon
                double moonAge = (DateTime.Now - new DateTime(2000, 1, 6, 18, 14, 0)).TotalDays % 29.53;
                bool isWaxing = moonAge < 14.765; // First half of lunar cycle

                // Calculate shadow offset based on illumination
                // 0% illumination = shadow fully covers moon (offset 0)
                // 100% illumination = shadow completely off moon (offset 40+)
                double shadowOffset = (illuminationPercent / 100.0) * 50; // Move shadow off by up to 50 pixels

                if (isWaxing)
                {
                    // Waxing: shadow moves from right to left (revealing right side first)
                    // Shadow starts covering the moon and slides left off of it
                    Canvas.SetLeft(moonShadow, shadowOffset - 10);
                }
                else
                {
                    // Waning: shadow moves from left to right (covering from right side)
                    // Shadow comes back from the right
                    Canvas.SetLeft(moonShadow, 10 - shadowOffset);
                }

                // Adjust glow intensity based on illumination
                if (moonGlow != null)
                {
                    moonGlow.Opacity = 0.1 + (illuminationPercent / 100.0 * 0.25);
                }

                // Adjust lit portion brightness
                moonLit.Opacity = 0.5 + (illuminationPercent / 100.0 * 0.5);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Moon phase update error: {ex.Message}");
            }
        }

        private void SetSkyColors(string color1, string color2, string color3, double opacity2, double opacity3)
        {
            try
            {
                var skyLayer1 = _page.FindName("SkyLayer1") as Rectangle;
                var skyLayer2 = _page.FindName("SkyLayer2") as Rectangle;
                var skyLayer3 = _page.FindName("SkyLayer3") as Rectangle;

                if (skyLayer1 == null || skyLayer2 == null || skyLayer3 == null)
                {
                    Debug.WriteLine("Sky layer elements not found");
                    return;
                }

                skyLayer1.Fill = new SolidColorBrush(
                    ColorHelper.FromArgb(255,
                        Convert.ToByte(color1.Substring(1, 2), 16),
                        Convert.ToByte(color1.Substring(3, 2), 16),
                        Convert.ToByte(color1.Substring(5, 2), 16)));

                skyLayer2.Fill = new SolidColorBrush(
                    ColorHelper.FromArgb(255,
                        Convert.ToByte(color2.Substring(1, 2), 16),
                        Convert.ToByte(color2.Substring(3, 2), 16),
                        Convert.ToByte(color2.Substring(5, 2), 16)));
                skyLayer2.Opacity = opacity2;

                skyLayer3.Fill = new SolidColorBrush(
                    ColorHelper.FromArgb(255,
                        Convert.ToByte(color3.Substring(1, 2), 16),
                        Convert.ToByte(color3.Substring(3, 2), 16),
                        Convert.ToByte(color3.Substring(5, 2), 16)));
                skyLayer3.Opacity = opacity3;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sky color update error: {ex.Message}");
            }
        }
    }
}
