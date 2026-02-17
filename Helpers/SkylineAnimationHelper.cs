using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;

namespace Jewochron.Helpers
{
    /// <summary>
    /// Manages animated characters walking across the Jerusalem skyline
    /// </summary>
    public class SkylineAnimationHelper
    {
        private readonly Page _page;
        private DispatcherQueueTimer? _camelMoveTimer;
        private DispatcherQueueTimer? _jewishManMoveTimer;

        public SkylineAnimationHelper(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public void AnimateCamelWalk()
        {
            var animatedCamel = _page.FindName("AnimatedCamel") as TextBlock;
            var camelTransform = _page.FindName("CamelTransform") as TranslateTransform;

            Debug.WriteLine("========================================");
            Debug.WriteLine($"[CAMEL] AnimateCamelWalk called at {DateTime.Now:HH:mm:ss}");

            if (animatedCamel == null || camelTransform == null)
            {
                Debug.WriteLine("[CAMEL] ERROR: Elements not found!");
                return;
            }

            try
            {
                // Stop any existing animation timer to prevent overlapping animations
                if (_camelMoveTimer != null && _camelMoveTimer.IsRunning)
                {
                    _camelMoveTimer.Stop();
                    Debug.WriteLine("[CAMEL] Stopped previous animation timer");
                }

                // Reset to start position
                camelTransform.X = 0;
                animatedCamel.Opacity = 1;

                Debug.WriteLine("[CAMEL] Starting animation");

                // Create timer for smooth movement
                _camelMoveTimer = _page.DispatcherQueue.CreateTimer();
                _camelMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                _camelMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX -= 2; // Move 2 pixels left each tick
                        camelTransform.X = currentX;

                        // After it goes off screen, stop and reset
                        if (currentX < -1300)
                        {
                            _camelMoveTimer?.Stop();
                            animatedCamel.Opacity = 0;
                            camelTransform.X = 0;
                            Debug.WriteLine("[CAMEL] Animation complete");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[CAMEL] Tick error: {ex.Message}");
                        _camelMoveTimer?.Stop();
                    }
                };

                _camelMoveTimer.Start();
                Debug.WriteLine("[CAMEL] Animation started");
                Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CAMEL] ERROR: {ex.Message}");
                Debug.WriteLine("========================================");
            }
        }

        public void AnimateJewishManWalk()
        {
            var animatedMan = _page.FindName("AnimatedJewishMan") as UIElement;
            var manTransform = _page.FindName("JewishManTransform") as TranslateTransform;

            Debug.WriteLine("========================================");
            Debug.WriteLine($"[JEWISH MAN] AnimateJewishManWalk called at {DateTime.Now:HH:mm:ss}");

            if (animatedMan == null || manTransform == null)
            {
                Debug.WriteLine("[JEWISH MAN] ERROR: Elements not found!");
                return;
            }

            try
            {
                // Stop any existing animation timer to prevent overlapping animations
                if (_jewishManMoveTimer != null && _jewishManMoveTimer.IsRunning)
                {
                    _jewishManMoveTimer.Stop();
                    Debug.WriteLine("[JEWISH MAN] Stopped previous animation timer");
                }

                // Reset to start position (left side)
                manTransform.X = 0;
                animatedMan.Opacity = 1;

                Debug.WriteLine("[JEWISH MAN] Starting animation");

                // Create timer for smooth movement (LEFT to RIGHT - opposite of camel)
                _jewishManMoveTimer = _page.DispatcherQueue.CreateTimer();
                _jewishManMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                _jewishManMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX += 2; // Move 2 pixels RIGHT each tick
                        manTransform.X = currentX;

                        // After he goes off screen, stop and reset
                        if (currentX > 1250)
                        {
                            _jewishManMoveTimer?.Stop();
                            animatedMan.Opacity = 0;
                            manTransform.X = 0;
                            Debug.WriteLine("[JEWISH MAN] Animation complete");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[JEWISH MAN] Tick error: {ex.Message}");
                        _jewishManMoveTimer?.Stop();
                    }
                };

                _jewishManMoveTimer.Start();
                Debug.WriteLine("[JEWISH MAN] Animation started");
                Debug.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JEWISH MAN] ERROR: {ex.Message}");
                Debug.WriteLine("========================================");
            }
        }

        public void StopAllAnimations()
        {
            _camelMoveTimer?.Stop();
            _jewishManMoveTimer?.Stop();
        }
    }
}
