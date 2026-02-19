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
        private DispatcherQueueTimer? _camelScheduleTimer;
        private DispatcherQueueTimer? _jewishManScheduleTimer;

        public SkylineAnimationHelper(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        /// <summary>
        /// Starts the animation timers for both camel and Jewish man
        /// </summary>
        public void StartAnimationTimers()
        {
            StartCamelTimer();
            StartJewishManTimer();
        }

        private void StartCamelTimer()
        {
            try
            {
                // Camel walks every 2 minutes
                _camelScheduleTimer = _page.DispatcherQueue.CreateTimer();
                _camelScheduleTimer.Interval = TimeSpan.FromMinutes(2);
                _camelScheduleTimer.Tick += (s, e) => AnimateCamelWalk();
                _camelScheduleTimer.Start();

                // Start first animation after 1 second
                var initialTimer = _page.DispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(1);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) => AnimateCamelWalk();
                initialTimer.Start();

                Debug.WriteLine("[ANIMATION] Camel timer initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ANIMATION] Failed to start camel timer: {ex.Message}");
            }
        }

        private void StartJewishManTimer()
        {
            try
            {
                // Jewish man walks every 3 minutes (offset from camel)
                _jewishManScheduleTimer = _page.DispatcherQueue.CreateTimer();
                _jewishManScheduleTimer.Interval = TimeSpan.FromMinutes(3);
                _jewishManScheduleTimer.Tick += (s, e) => AnimateJewishManWalk();
                _jewishManScheduleTimer.Start();

                // Start first animation after 30 seconds
                var initialTimer = _page.DispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(30);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) => AnimateJewishManWalk();
                initialTimer.Start();

                Debug.WriteLine("[ANIMATION] Jewish man timer initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ANIMATION] Failed to start Jewish man timer: {ex.Message}");
            }
        }

        public void AnimateCamelWalk()
        {
            var animatedCamel = _page.FindName("AnimatedCamel") as TextBlock;
            var camelTransform = _page.FindName("CamelTransform") as TranslateTransform;

            if (animatedCamel == null || camelTransform == null)
            {
                Debug.WriteLine("[CAMEL] Elements not found");
                return;
            }

            try
            {
                // Stop any existing animation
                if (_camelMoveTimer != null && _camelMoveTimer.IsRunning)
                {
                    _camelMoveTimer.Stop();
                }

                // Reset to start position
                camelTransform.X = 0;
                animatedCamel.Opacity = 1;

                // Create timer for smooth movement
                _camelMoveTimer = _page.DispatcherQueue.CreateTimer();
                _camelMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                _camelMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX -= 2;
                        camelTransform.X = currentX;

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CAMEL] Animation error: {ex.Message}");
            }
        }

        public void AnimateJewishManWalk()
        {
            var animatedMan = _page.FindName("AnimatedJewishMan") as UIElement;
            var manTransform = _page.FindName("JewishManTransform") as TranslateTransform;

            if (animatedMan == null || manTransform == null)
            {
                Debug.WriteLine("[JEWISH MAN] Elements not found");
                return;
            }

            try
            {
                // Stop any existing animation
                if (_jewishManMoveTimer != null && _jewishManMoveTimer.IsRunning)
                {
                    _jewishManMoveTimer.Stop();
                }

                // Reset to start position
                manTransform.X = 0;
                animatedMan.Opacity = 1;

                // Create timer for smooth movement (LEFT to RIGHT)
                _jewishManMoveTimer = _page.DispatcherQueue.CreateTimer();
                _jewishManMoveTimer.Interval = TimeSpan.FromMilliseconds(60);
                double currentX = 0;

                _jewishManMoveTimer.Tick += (s, e) =>
                {
                    try
                    {
                        currentX += 2;
                        manTransform.X = currentX;

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[JEWISH MAN] Animation error: {ex.Message}");
            }
        }

        public void StopAllAnimations()
        {
            _camelMoveTimer?.Stop();
            _jewishManMoveTimer?.Stop();
            _camelScheduleTimer?.Stop();
            _jewishManScheduleTimer?.Stop();
        }
    }
}
