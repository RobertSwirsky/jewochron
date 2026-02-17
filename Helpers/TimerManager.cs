using Microsoft.UI.Dispatching;
using System;
using System.Diagnostics;

namespace Jewochron.Helpers
{
    /// <summary>
    /// Manages all application timers (clock, data refresh, animations)
    /// </summary>
    public class TimerManager
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private DispatcherQueueTimer? _clockTimer;
        private DispatcherQueueTimer? _dataRefreshTimer;
        private DispatcherQueueTimer? _camelTimer;
        private DispatcherQueueTimer? _jewishManTimer;

        public TimerManager(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        public void StartClockTimer(Action updateClocksAction)
        {
            _clockTimer = _dispatcherQueue.CreateTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += (s, e) =>
            {
                try
                {
                    updateClocksAction();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Clock update error: {ex.Message}");
                }
            };
            _clockTimer.Start();

            // Update immediately
            try
            {
                updateClocksAction();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Initial clock update error: {ex.Message}");
            }
        }

        public void StartDataRefreshTimer(Func<System.Threading.Tasks.Task> checkAndRefreshDataAction)
        {
            _dataRefreshTimer = _dispatcherQueue.CreateTimer();
            _dataRefreshTimer.Interval = TimeSpan.FromMinutes(1);
            _dataRefreshTimer.Tick += async (s, e) =>
            {
                try
                {
                    await checkAndRefreshDataAction();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Data refresh error: {ex.Message}");
                }
            };
            _dataRefreshTimer.Start();
        }

        public void StartCamelAnimationTimer(Action animateCamelAction)
        {
            try
            {
                // Camel walks every 2 minutes
                _camelTimer = _dispatcherQueue.CreateTimer();
                _camelTimer.Interval = TimeSpan.FromMinutes(2);
                _camelTimer.Tick += (s, e) =>
                {
                    try
                    {
                        animateCamelAction();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Camel animation error: {ex.Message}");
                    }
                };
                _camelTimer.Start();

                // Start first animation after 1 second
                var initialTimer = _dispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(1);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) =>
                {
                    try
                    {
                        Debug.WriteLine("Starting INITIAL camel animation");
                        animateCamelAction();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Initial camel animation error: {ex.Message}");
                    }
                };
                initialTimer.Start();
                Debug.WriteLine("Camel animation timer initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start camel animation: {ex.Message}");
            }
        }

        public void StartJewishManAnimationTimer(Action animateJewishManAction)
        {
            try
            {
                // Jewish man walks every 3 minutes (offset from camel)
                _jewishManTimer = _dispatcherQueue.CreateTimer();
                _jewishManTimer.Interval = TimeSpan.FromMinutes(3);
                _jewishManTimer.Tick += (s, e) =>
                {
                    try
                    {
                        animateJewishManAction();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Jewish man animation error: {ex.Message}");
                    }
                };
                _jewishManTimer.Start();

                // Start first animation after 30 seconds (offset from camel)
                var initialTimer = _dispatcherQueue.CreateTimer();
                initialTimer.Interval = TimeSpan.FromSeconds(30);
                initialTimer.IsRepeating = false;
                initialTimer.Tick += (s, e) =>
                {
                    try
                    {
                        Debug.WriteLine("Starting INITIAL Jewish man animation");
                        animateJewishManAction();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Initial Jewish man animation error: {ex.Message}");
                    }
                };
                initialTimer.Start();
                Debug.WriteLine("Jewish man animation timer initialized");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to start Jewish man animation: {ex.Message}");
            }
        }

        public void StopAllTimers()
        {
            _clockTimer?.Stop();
            _dataRefreshTimer?.Stop();
            _camelTimer?.Stop();
            _jewishManTimer?.Stop();
        }
    }
}
