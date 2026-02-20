using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Jewochron.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        private void ManageYahrzeits_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(YahrzeitPage));
        }

        private void ManageSimchas_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SimchasPage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
