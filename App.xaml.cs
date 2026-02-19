using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Dispatching;
using Jewochron.Views;
using Jewochron.Services;

namespace Jewochron
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? window;
        private YahrzeitWebServer? webServer;
        private DispatcherQueue? dispatcherQueue;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            window ??= new Window();

            // Capture the dispatcher queue for UI thread access
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            if (window.Content is not Frame rootFrame)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                window.Content = rootFrame;
            }

            _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
            window.Activate();

            // Start the web server for Yahrzeit management
            // SQLite database will be stored in the app's local data folder
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbFolder = Path.Combine(appDataPath, "Jewochron");
            Directory.CreateDirectory(dbFolder); // Ensure folder exists
            string dbPath = Path.Combine(dbFolder, "yahrzeits.db");

            webServer = new YahrzeitWebServer(dbPath);
            webServer.YahrzeitDataChanged += OnYahrzeitDataChanged;

            try
            {
                await webServer.StartAsync();
                System.Diagnostics.Debug.WriteLine($"Yahrzeit web interface available at http://localhost:5555");
                System.Diagnostics.Debug.WriteLine($"Database location: {dbPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start web server: {ex.Message}");
            }
        }

        private void OnYahrzeitDataChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[YAHRZEIT] Event received in App.xaml.cs");
            // Dispatch to UI thread and refresh the yahrzeit display
            var enqueued = dispatcherQueue?.TryEnqueue(async () =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[YAHRZEIT] Dispatched to UI thread");
                    if (window?.Content is Frame frame && frame.Content is MainPage mainPage)
                    {
                        await mainPage.RefreshYahrzeitsAsync();
                        System.Diagnostics.Debug.WriteLine("[YAHRZEIT] UI refreshed after data change");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[YAHRZEIT] Could not find MainPage");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] Error refreshing UI: {ex.Message}");
                }
            });
            System.Diagnostics.Debug.WriteLine($"[YAHRZEIT] TryEnqueue result: {enqueued}");
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
