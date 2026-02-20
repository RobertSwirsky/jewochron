using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Jewochron.Services;
using Jewochron.Models;
using System.Collections.ObjectModel;

namespace Jewochron.Views
{
    public sealed partial class YahrzeitPage : Page
    {
        private YahrzeitService? yahrzeitService;
        private HebrewCalendarService? hebrewCalendarService;
        private int selectedHebrewDay;
        private int selectedHebrewMonth;
        private int selectedHebrewYear;
        private string selectedHebrewDateString = "";
        
        public ObservableCollection<YahrzeitDisplayItem> Yahrzeits { get; set; } = new();

        public YahrzeitPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            hebrewCalendarService = new HebrewCalendarService();
            
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(appDataPath, "Jewochron", "yahrzeits.db");
            yahrzeitService = new YahrzeitService(dbPath, hebrewCalendarService);

            InitializeDatePickers();
            await LoadYahrzeits();
        }

        private void InitializeDatePickers()
        {
            if (hebrewCalendarService == null) return;

            for (int i = 1; i <= 30; i++)
            {
                YahrzeitDayComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }

            var months = new[]
            {
                (1, "Tishrei"), (2, "Cheshvan"), (3, "Kislev"), (4, "Tevet"),
                (5, "Shevat"), (6, "Adar"), (7, "Nisan"), (8, "Iyar"),
                (9, "Sivan"), (10, "Tammuz"), (11, "Av"), (12, "Elul")
            };
            foreach (var (num, name) in months)
            {
                YahrzeitMonthComboBox.Items.Add(new ComboBoxItem { Content = name, Tag = num });
            }

            var currentYear = DateTime.Today.Year;
            var hebrewCalendar = new System.Globalization.HebrewCalendar();
            var currentHebrewYear = hebrewCalendar.GetYear(DateTime.Today);

            for (int i = currentHebrewYear - 120; i <= currentHebrewYear; i++)
            {
                YahrzeitYearComboBox.Items.Add(new ComboBoxItem { Content = i.ToString(), Tag = i });
            }

            var today = DateTime.Today;
            var hebrewDate = hebrewCalendarService.ConvertToHebrewDate(today);
            
            if (hebrewDate.HasValue)
            {
                YahrzeitDayComboBox.SelectedValue = hebrewDate.Value.day;
                YahrzeitMonthComboBox.SelectedValue = hebrewDate.Value.month;
                YahrzeitYearComboBox.SelectedValue = hebrewDate.Value.year;
                UpdateYahrzeitDateDisplay();
            }
        }

        private void UpdateYahrzeitDateDisplay()
        {
            if (YahrzeitDayComboBox.SelectedItem is ComboBoxItem dayItem &&
                YahrzeitMonthComboBox.SelectedItem is ComboBoxItem monthItem &&
                YahrzeitYearComboBox.SelectedItem is ComboBoxItem yearItem &&
                hebrewCalendarService != null)
            {
                selectedHebrewDay = (int)dayItem.Tag;
                selectedHebrewMonth = (int)monthItem.Tag;
                selectedHebrewYear = (int)yearItem.Tag;

                var formatted = hebrewCalendarService.FormatHebrewDate(selectedHebrewDay, selectedHebrewMonth, selectedHebrewYear);
                selectedHebrewDateString = formatted.hebrew;
                YahrzeitSelectedDateDisplay.Text = $"{formatted.english}\n{formatted.hebrew}";
            }
        }

        private async Task LoadYahrzeits()
        {
            if (yahrzeitService == null || hebrewCalendarService == null) return;

            var yahrzeits = await yahrzeitService.GetAllYahrzeitsAsync();
            Yahrzeits.Clear();

            foreach (var yahrzeit in yahrzeits)
            {
                var nextOccurrence = GetNextOccurrence(yahrzeit);
                var hebrewDateString = hebrewCalendarService.FormatHebrewDate(yahrzeit.HebrewDay, yahrzeit.HebrewMonth, yahrzeit.HebrewYear);
                
                var displayItem = new YahrzeitDisplayItem
                {
                    Id = yahrzeit.Id,
                    EnglishName = yahrzeit.NameEnglish,
                    HebrewName = yahrzeit.NameHebrew,
                    HebrewDate = hebrewDateString.english,
                    NextOccurrence = nextOccurrence?.ToString("dddd, MMMM d, yyyy") ?? "Unable to calculate"
                };
                Yahrzeits.Add(displayItem);
            }
        }

        private DateTime? GetNextOccurrence(Yahrzeit yahrzeit)
        {
            if (hebrewCalendarService == null) return null;
            return hebrewCalendarService.GetNextHebrewAnniversary(yahrzeit.HebrewDay, yahrzeit.HebrewMonth, yahrzeit.HebrewYear);
        }

        private void YahrzeitDayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateYahrzeitDateDisplay();
        }

        private void YahrzeitMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateYahrzeitDateDisplay();
        }

        private void YahrzeitYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateYahrzeitDateDisplay();
        }

        private async void AddYahrzeit_Click(object sender, RoutedEventArgs e)
        {
            if (yahrzeitService == null || hebrewCalendarService == null) return;

            if (string.IsNullOrWhiteSpace(txtNameEnglish.Text) || string.IsNullOrWhiteSpace(txtNameHebrew.Text) ||
                cmbGender.SelectedItem is not ComboBoxItem || selectedHebrewDay == 0 || selectedHebrewMonth == 0 || selectedHebrewYear == 0)
            {
                return;
            }

            var yahrzeit = new Yahrzeit
            {
                NameEnglish = txtNameEnglish.Text.Trim(),
                NameHebrew = txtNameHebrew.Text.Trim(),
                Gender = (cmbGender.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "M",
                HebrewDay = selectedHebrewDay,
                HebrewMonth = selectedHebrewMonth,
                HebrewYear = selectedHebrewYear
            };

            var success = await yahrzeitService.AddYahrzeitAsync(yahrzeit);
            if (success)
            {
                txtNameEnglish.Text = "";
                txtNameHebrew.Text = "";
                cmbGender.SelectedIndex = -1;
                
                await LoadYahrzeits();
            }
        }

        private async void DeleteYahrzeit_Click(object sender, RoutedEventArgs e)
        {
            if (yahrzeitService == null) return;

            if (sender is Button button && button.Tag is int id)
            {
                var success = await yahrzeitService.DeleteYahrzeitAsync(id);
                if (success)
                {
                    await LoadYahrzeits();
                }
            }
        }

        private void YahrzeitsList_ItemClick(object sender, ItemClickEventArgs e)
        {
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }

    public class YahrzeitDisplayItem
    {
        public int Id { get; set; }
        public string EnglishName { get; set; } = "";
        public string HebrewName { get; set; } = "";
        public string HebrewDate { get; set; } = "";
        public string NextOccurrence { get; set; } = "";
    }
}
